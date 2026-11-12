
using Microsoft.EntityFrameworkCore;
using Souqify.Application.Helpers;
using Souqify.Domain.Entities;
using Souqify.Domain.Entities.Enums;
using Souqify.Infrastructure.Seeding.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Souqify.Infrastructure.Seeding
{
    // Fills the spec tables from SeedData/spec-definitions.json when the app starts.
    // Add-only: a row that is already in the database is skipped. Nothing is changed or deleted.
    public class SpecSeeder
    {
        private readonly SouqifyDbContext _souqifyDbContext;

        public SpecSeeder(SouqifyDbContext souqifyDbContext)
        {
            _souqifyDbContext = souqifyDbContext;
        }

        public async Task SeedAsync()
        {
            var seed = await ReadSeedFileAsync();

            // Pass 1
            var categoriesBySlug = await SeedCategoriesAsync(seed.Categories);

            // Pass 2
            var specAttributesByKey = await SeedSpecAttributesAsync(seed.Specs);

            // Pass 3
            await SeedSpecOptionsAsync(seed.Specs, specAttributesByKey);

            // Pass 4
            await SeedCategorySpecsAsync(seed.Forms, categoriesBySlug, specAttributesByKey);

            // One save for all passes. If any pass throws before this line, nothing is saved.
            await _souqifyDbContext.SaveChangesAsync();
        }

        private static async Task<SpecDefinitionsSeed> ReadSeedFileAsync()
        {
            // The .json file is copied next to the app's .dll: bin/... in development, the publish folder in production.
            var path = Path.Combine(AppContext.BaseDirectory, "SeedData", "spec-definitions.json");

            if (!File.Exists(path))
                throw new InvalidOperationException($"Spec seed: file not found at {path}");

            var json = await File.ReadAllTextAsync(path);

            var options = new JsonSerializerOptions
            {
                // Trap 1: "slug" in the file must fill Slug in the class.
                PropertyNameCaseInsensitive = true,
            };

            // Trap 2: "Option" in the file must become SpecDataType.Option.
            options.Converters.Add(new JsonStringEnumConverter());

            return JsonSerializer.Deserialize<SpecDefinitionsSeed>(json, options)
                ?? throw new InvalidOperationException("Spec seed: the file has no data (it contains only null).");
        }

        // Pass 1: categories, found by slug.
        // Returns every category (old and new) by slug, because pass 4 needs it to find each form's category.
        private async Task<Dictionary<string, Category>> SeedCategoriesAsync(List<CategorySeed> categorySeeds)
        {
            // All categories in the database, by slug: one query.
            // New categories are added to it below, so a child further down the file can find a parent that is new in this run.
            var categoriesBySlug = await _souqifyDbContext.Categories.ToDictionaryAsync(c => c.Slug);

            foreach (var categorySeed in categorySeeds)
            {
                // A field left out in the file is null here (see the = null! in CategorySeed).
                if (string.IsNullOrWhiteSpace(categorySeed.Name) || string.IsNullOrWhiteSpace(categorySeed.Slug))
                    throw new InvalidOperationException("Spec seed: every category needs a name and a slug.");

                // Already there: skip it. This is what makes a second run add nothing.
                if (categoriesBySlug.ContainsKey(categorySeed.Slug))
                    continue;

                // The slug must already be clean: "graphic-cards", not "Graphic Cards".
                if (SlugHelper.Create(categorySeed.Slug) != categorySeed.Slug)
                    throw new InvalidOperationException($"Spec seed: category slug '{categorySeed.Slug}' is not valid. Use a-z, 0-9 and dashes only.");

                Guid? parentId = null;

                if (categorySeed.Parent != null)
                {
                    // The parent must be in the database already, or higher up in the file.
                    if (!categoriesBySlug.TryGetValue(categorySeed.Parent, out var parent))
                        throw new InvalidOperationException($"Spec seed: category '{categorySeed.Slug}' has parent '{categorySeed.Parent}', which was not found. Put the parent above the child in the file.");

                    // Same two rules as AddCategoryAsync.
                    if (await _souqifyDbContext.Products.AnyAsync(p => p.CategoryId == parent.Id))
                        throw new InvalidOperationException($"Spec seed: parent '{parent.Slug}' already holds products, so it can't get the child '{categorySeed.Slug}'.");

                    if (await _souqifyDbContext.CategorySpecs.AnyAsync(cs => cs.CategoryId == parent.Id))
                        throw new InvalidOperationException($"Spec seed: parent '{parent.Slug}' has a spec form, so it can't get the child '{categorySeed.Slug}'.");

                    parentId = parent.Id;
                }

                // Same rule as the unique index on (ParentId, Name): two categories under one parent can't share a name.
                if (categoriesBySlug.Values.Any(c => c.ParentId == parentId && c.Name == categorySeed.Name))
                    throw new InvalidOperationException($"Spec seed: a category named '{categorySeed.Name}' already exists under this parent.");

                var categoryEnt = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = categorySeed.Name,
                    Slug = categorySeed.Slug,
                    ParentId = parentId,
                    DisplayOrder = categorySeed.DisplayOrder,
                    Description = categorySeed.Description,
                };

                _souqifyDbContext.Categories.Add(categoryEnt);

                categoriesBySlug.Add(categoryEnt.Slug, categoryEnt);
            }

            return categoriesBySlug;
        }

        // Pass 2: questions, found by key.
        // Same rules as POST /specs (CreateSpecAttributeValidator + CreateSpecAttributeAsync).
        // Returns every question (old and new) by key: pass 3 needs it for the choices, pass 4 for the form rows.
        private async Task<Dictionary<string, SpecAttribute>> SeedSpecAttributesAsync(List<SpecSeed> specSeeds)
        {
            // All questions in the database, by key: one query. New ones are added to it below.
            var specAttributesByKey = await _souqifyDbContext.SpecAttributes.ToDictionaryAsync(sa => sa.Key);

            foreach (var specSeed in specSeeds)
            {
                // A field left out in the file is null here (see the = null! in SpecSeed).
                if (string.IsNullOrWhiteSpace(specSeed.Key) || string.IsNullOrWhiteSpace(specSeed.Name))
                    throw new InvalidOperationException("Spec seed: every question needs a key and a name.");

                // Already there: skip it. In POST /specs this is a 400, here it's just a restart.
                if (specAttributesByKey.ContainsKey(specSeed.Key))
                    continue;

                // Same regex and max length as CreateSpecAttributeValidator: "vram_gb", not "VRAM GB" or "_vram".
                if (specSeed.Key.Length > 50 || !Regex.IsMatch(specSeed.Key, "^[a-z0-9]+(_[a-z0-9]+)*$"))
                    throw new InvalidOperationException($"Spec seed: question key '{specSeed.Key}' is not valid. Use lower-case letters, digits and single underscores, max 50.");

                // The converter also accepts numbers, so "dataType": 7 would get through as a value that doesn't exist.
                if (!Enum.IsDefined(specSeed.DataType))
                    throw new InvalidOperationException($"Spec seed: question '{specSeed.Key}' has an unknown dataType.");

                // Same clean-up as CreateSpecAttributeAsync: no spaces around the name, and a blank unit means "no unit" (null).
                var name = specSeed.Name.Trim();
                var unit = string.IsNullOrWhiteSpace(specSeed.Unit) ? null : specSeed.Unit.Trim();

                if (name.Length > 50)
                    throw new InvalidOperationException($"Spec seed: question '{specSeed.Key}' has a name longer than 50.");

                if (unit != null && unit.Length > 20)
                    throw new InvalidOperationException($"Spec seed: question '{specSeed.Key}' has a unit longer than 20.");

                var specAttributeEnt = new SpecAttribute
                {
                    Id = Guid.NewGuid(),
                    Key = specSeed.Key,
                    Name = name,
                    DataType = specSeed.DataType,
                    Unit = unit,
                };

                _souqifyDbContext.SpecAttributes.Add(specAttributeEnt);

                // Pass 3 and pass 4 find new questions here, before they are saved.
                specAttributesByKey.Add(specAttributeEnt.Key, specAttributeEnt);
            }

            return specAttributesByKey;
        }

        // Pass 3: choices, found by (question, slug).
        // Same rules as POST /specs/{specId}/options (CreateSpecOptionAsync).
        // Runs for every question in the file, also the ones pass 2 skipped, so a new choice on an old question is added too.
        private async Task SeedSpecOptionsAsync(List<SpecSeed> specSeeds, Dictionary<string, SpecAttribute> specAttributesByKey)
        {
            // Every choice that already exists, as a (question Id, slug) pair: one query.
            // A slug alone isn't unique ("16" can be a choice of two questions). The pair is, like the unique index on (SpecAttributeId, Slug).
            var existingRows = await _souqifyDbContext.SpecOptions
                .Select(so => new { so.SpecAttributeId, so.Slug })
                .ToListAsync();

            var knownOptions = new HashSet<(Guid SpecAttributeId, string Slug)>();

            foreach (var row in existingRows)
            {
                knownOptions.Add((row.SpecAttributeId, row.Slug));
            }

            foreach (var specSeed in specSeeds)
            {
                // No "options" in the file = an empty list: nothing to add for this question.
                if (specSeed.Options.Count == 0)
                    continue;

                // The real entity: loaded from the database, or built in pass 2 (with its Id already made).
                // Safe to use []: pass 2 went through the same list, so every key is in the dictionary.
                var specAttribute = specAttributesByKey[specSeed.Key];

                // Same rule as CreateSpecOptionAsync. We check the entity's DataType, because that's the question the choices will belong to.
                if (specAttribute.DataType != SpecDataType.Option && specAttribute.DataType != SpecDataType.MultiOption)
                    throw new InvalidOperationException($"Spec seed: question '{specSeed.Key}' is a {specAttribute.DataType} question. Only Option and MultiOption questions can have choices.");

                // The slugs of this question's list, to catch the same choice written twice ("RTX 5070" and "RTX-5070" both give "rtx-5070").
                var slugsInThisList = new HashSet<string>();

                var position = 0;

                foreach (var optionValue in specSeed.Options)
                {
                    // Counted for every line, also the skipped ones, so the order always matches the position in the file.
                    position++;

                    if (string.IsNullOrWhiteSpace(optionValue))
                        throw new InvalidOperationException($"Spec seed: question '{specSeed.Key}' has an empty choice at position {position}.");

                    // Same clean-up and slug as CreateSpecOptionAsync.
                    var value = optionValue.Trim();
                    var slug = SlugHelper.Create(value);

                    if (value.Length > 50)
                        throw new InvalidOperationException($"Spec seed: choice '{value}' of '{specSeed.Key}' is longer than 50.");

                    // A value with no a-z or 0-9 (for example Arabic) gives an empty slug.
                    if (slug.Length == 0)
                        throw new InvalidOperationException($"Spec seed: choice '{value}' of '{specSeed.Key}' needs at least one English letter or digit.");

                    // Add returns false when the slug is already in the set: the file lists the same choice twice.
                    if (!slugsInThisList.Add(slug))
                        throw new InvalidOperationException($"Spec seed: question '{specSeed.Key}' lists the choice '{slug}' twice.");

                    // Already there: skip it.
                    if (knownOptions.Contains((specAttribute.Id, slug)))
                        continue;

                    // No Id here: nothing in the seeder points to a choice, so EF makes the Id on Add (like CreateSpecOptionAsync).
                    var specOptionEnt = new SpecOption
                    {
                        SpecAttributeId = specAttribute.Id,
                        Value = value,
                        Slug = slug,
                        DisplayOrder = position,
                    };

                    _souqifyDbContext.SpecOptions.Add(specOptionEnt);

                    knownOptions.Add((specAttribute.Id, slug));
                }
            }
        }

        // Pass 4: form rows, found by (category, question).
        // Same rules as POST /categories/{categoryId}/specs (CreateCategorySpecAsync).
        // Needs both dictionaries: one row = one category (pass 1) + one question (pass 2).
        private async Task SeedCategorySpecsAsync(List<FormSeed> formSeeds, Dictionary<string, Category> categoriesBySlug, Dictionary<string, SpecAttribute> specAttributesByKey)
        {
            // Every form row that already exists, as a (category Id, question Id) pair: one query.
            // The pair is CategorySpec's key (HasKey(CategoryId, SpecAttributeId)).
            var existingRows = await _souqifyDbContext.CategorySpecs
                .Select(cs => new { cs.CategoryId, cs.SpecAttributeId })
                .ToListAsync();

            var knownRows = new HashSet<(Guid CategoryId, Guid SpecAttributeId)>();

            foreach (var row in existingRows)
            {
                knownRows.Add((row.CategoryId, row.SpecAttributeId));
            }

            // The categories that already had a form in the file, to catch the same form written twice.
            var formCategories = new HashSet<string>();

            foreach (var formSeed in formSeeds)
            {
                if (string.IsNullOrWhiteSpace(formSeed.Category))
                    throw new InvalidOperationException("Spec seed: every form needs a category.");

                if (!formCategories.Add(formSeed.Category))
                    throw new InvalidOperationException($"Spec seed: the form for '{formSeed.Category}' is written twice. Put all its rows in one form.");

                // The category must be in the database, or in the file's categories (pass 1 added it to the dictionary).
                if (!categoriesBySlug.TryGetValue(formSeed.Category, out var category))
                    throw new InvalidOperationException($"Spec seed: the form's category '{formSeed.Category}' was not found. Add it to \"categories\" first.");

                // Same rule as CreateCategorySpecAsync: only a last-level category can have a form.
                // The dictionary holds every category, old and new, so this also sees children that pass 1 added in this run (not saved yet).
                if (categoriesBySlug.Values.Any(c => c.ParentId == category.Id))
                    throw new InvalidOperationException($"Spec seed: '{formSeed.Category}' has subcategories. Only a last-level category can have a spec form.");

                // The keys of this form, to catch the same question written twice.
                var keysInThisForm = new HashSet<string>();

                var position = 0;

                foreach (var formRowSeed in formSeed.Specs)
                {
                    // Counted for every row, also the skipped ones, so the order always matches the position in the file.
                    position++;

                    if (string.IsNullOrWhiteSpace(formRowSeed.Key))
                        throw new InvalidOperationException($"Spec seed: the form for '{formSeed.Category}' has a row with no key at position {position}.");

                    if (!keysInThisForm.Add(formRowSeed.Key))
                        throw new InvalidOperationException($"Spec seed: the form for '{formSeed.Category}' lists '{formRowSeed.Key}' twice.");

                    // The question must be in the database, or in the file's specs (pass 2 added it to the dictionary).
                    if (!specAttributesByKey.TryGetValue(formRowSeed.Key, out var specAttribute))
                        throw new InvalidOperationException($"Spec seed: the form for '{formSeed.Category}' uses '{formRowSeed.Key}', which was not found. Add it to \"specs\" first.");

                    // Same rule as CreateCategorySpecAsync: free text can't be a filter (there's nothing to tick).
                    if (specAttribute.DataType == SpecDataType.Text && formRowSeed.Filterable)
                        throw new InvalidOperationException($"Spec seed: '{formRowSeed.Key}' is a Text question, so it can't be filterable (form '{formSeed.Category}').");

                    // Already on this form: skip it. To change an existing row, use PUT /categories/{categoryId}/specs/{specId}.
                    if (knownRows.Contains((category.Id, specAttribute.Id)))
                        continue;

                    var categorySpecEnt = new CategorySpec
                    {
                        CategoryId = category.Id,
                        SpecAttributeId = specAttribute.Id,
                        IsRequired = formRowSeed.Required,
                        IsFilterable = formRowSeed.Filterable,
                        DisplayOrder = position,
                    };

                    _souqifyDbContext.CategorySpecs.Add(categorySpecEnt);

                    knownRows.Add((category.Id, specAttribute.Id));
                }
            }
        }
    }
}
