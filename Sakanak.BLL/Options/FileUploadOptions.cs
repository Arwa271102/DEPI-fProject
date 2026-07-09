namespace Sakanak.BLL.Options;

public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    public int MaxFileSizeInMB { get; set; } = 5;
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png"];
    public string ApartmentPhotosPath { get; set; } = "wwwroot/uploads/apartments";
    public string ProfilePhotosPath { get; set; } = "wwwroot/uploads/profiles";
    public int MaxProfilePhotoSizeMB { get; set; } = 5;
    public int MaxPhotosPerApartment { get; set; } = 10;
}
