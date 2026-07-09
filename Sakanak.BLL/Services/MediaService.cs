using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.BLL.Interfaces;
using Sakanak.BLL.Options;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class MediaService : IMediaService
{
    public const string ApartmentEntityType = "Apartment";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;
    private readonly IMapper _mapper;
    private readonly FileUploadOptions _fileUploadOptions;

    public MediaService(
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment,
        IMapper mapper,
        IOptions<FileUploadOptions> fileUploadOptions)
    {
        _unitOfWork = unitOfWork;
        _environment = environment;
        _mapper = mapper;
        _fileUploadOptions = fileUploadOptions.Value;
    }

    public Result ValidateApartmentFiles(IEnumerable<IFormFile> files)
    {
        var fileList = files.Where(file => file.Length > 0).ToList();
        if (fileList.Count == 0)
        {
            return Result.Failure("At least one valid image is required.");
        }

        if (fileList.Count > _fileUploadOptions.MaxPhotosPerApartment)
        {
            return Result.Failure($"A maximum of {_fileUploadOptions.MaxPhotosPerApartment} photos is allowed.");
        }

        var maxFileSizeInBytes = _fileUploadOptions.MaxFileSizeInMB * 1024 * 1024;
        var allowedExtensions = _fileUploadOptions.AllowedExtensions.Select(extension => extension.ToLowerInvariant()).ToHashSet();

        foreach (var file in fileList)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return Result.Failure($"File '{file.FileName}' has an unsupported extension.");
            }

            if (file.Length > maxFileSizeInBytes)
            {
                return Result.Failure($"File '{file.FileName}' exceeds the {_fileUploadOptions.MaxFileSizeInMB}MB limit.");
            }
        }

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<ApartmentMediaDto>>> UploadApartmentPhotosAsync(int apartmentId, IEnumerable<IFormFile> files)
    {
        var fileList = files.Where(file => file.Length > 0).ToList();
        var validationResult = ValidateApartmentFiles(fileList);
        if (!validationResult.Succeeded)
        {
            return Result<IReadOnlyList<ApartmentMediaDto>>.Failure(validationResult.Errors);
        }

        var apartmentFolderPath = EnsureApartmentDirectory(apartmentId);
        var mediaItems = new List<Media>();
        var writtenFiles = new List<string>();

        try
        {
            foreach (var file in fileList)
            {
                var uniqueFileName = $"{Guid.NewGuid():N}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
                var absolutePath = Path.Combine(apartmentFolderPath, uniqueFileName);

                await using (var stream = new FileStream(absolutePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                writtenFiles.Add(absolutePath);
                var relativeUrl = $"/uploads/apartments/{apartmentId}/{uniqueFileName}";
                var media = new Media
                {
                    EntityId = apartmentId,
                    ApartmentId = apartmentId, // Set the foreign key for navigation
                    EntityType = ApartmentEntityType,
                    Type = MediaType.Image,
                    Url = relativeUrl
                };

                await _unitOfWork.Media.AddAsync(media);
                mediaItems.Add(media);
            }

            await _unitOfWork.SaveChangesAsync();
            return Result<IReadOnlyList<ApartmentMediaDto>>.Success(_mapper.Map<IReadOnlyList<ApartmentMediaDto>>(mediaItems));
        }
        catch
        {
            foreach (var filePath in writtenFiles.Where(File.Exists))
            {
                File.Delete(filePath);
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<ApartmentMediaDto>> GetApartmentMediaAsync(int apartmentId)
    {
        var media = await _unitOfWork.Media.GetImagesByEntityAsync(ApartmentEntityType, apartmentId);
        return _mapper.Map<IReadOnlyList<ApartmentMediaDto>>(media);
    }

    public async Task<Result> DeleteMediaAsync(int mediaId)
    {
        var media = await _unitOfWork.Media.GetByIdAsync(mediaId);
        if (media is null)
        {
            return Result.Failure("Photo not found.");
        }

        DeletePhysicalFile(media.Url);
        await _unitOfWork.Media.DeleteAsync(mediaId);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteApartmentMediaAsync(int apartmentId)
    {
        var mediaItems = await _unitOfWork.Media.GetByEntityAsync(ApartmentEntityType, apartmentId);
        foreach (var media in mediaItems)
        {
            DeletePhysicalFile(media.Url);
        }

        await _unitOfWork.Media.DeleteByEntityAsync(ApartmentEntityType, apartmentId);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    private string EnsureApartmentDirectory(int apartmentId)
    {
        var apartmentsRoot = GetApartmentUploadsRootPath();
        var apartmentDirectory = Path.Combine(apartmentsRoot, apartmentId.ToString());
        Directory.CreateDirectory(apartmentDirectory);
        return apartmentDirectory;
    }

    private void DeletePhysicalFile(string relativeUrl)
    {
        var sanitizedPath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var normalizedPrefix = $"uploads{Path.DirectorySeparatorChar}";
        if (sanitizedPath.StartsWith($"wwwroot{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            sanitizedPath = sanitizedPath[(($"wwwroot{Path.DirectorySeparatorChar}").Length)..];
        }

        if (!sanitizedPath.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            sanitizedPath = Path.Combine("uploads", "apartments", Path.GetFileName(sanitizedPath));
        }

        var absolutePath = Path.Combine(_environment.WebRootPath, sanitizedPath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    private string GetApartmentUploadsRootPath()
    {
        var configuredPath = _fileUploadOptions.ApartmentPhotosPath.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        if (configuredPath.StartsWith($"wwwroot{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            configuredPath = configuredPath[(($"wwwroot{Path.DirectorySeparatorChar}").Length)..];
        }

        return Path.Combine(_environment.WebRootPath, configuredPath);
    }
}
