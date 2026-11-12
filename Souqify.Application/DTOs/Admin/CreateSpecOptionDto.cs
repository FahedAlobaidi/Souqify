
namespace Souqify.Application.DTOs.Admin
{
    public class CreateSpecOptionDto
    {
        public int DisplayOrder { get; set; }
        // Not "required": a missing value must reach CreateSpecOptionValidator (NotEmpty), so the 400 looks like every other one.
        public string Value { get; set; } = null!;
    }
}
