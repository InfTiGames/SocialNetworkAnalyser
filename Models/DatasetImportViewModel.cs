using System.ComponentModel.DataAnnotations;

namespace SocialNetworkAnalyser.Models;

public record DatasetImportViewModel
{
    [Required(ErrorMessage = "Dataset name is required.")]
    public string DatasetName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Please upload a valid file.")]
    public IFormFile? UploadFile { get; init; }
}