namespace Sakanak.BLL.DTOs;

public class Result
{
    public bool Succeeded { get; set; }
    public List<string> Errors { get; set; } = new();

    public bool IsSuccess => Succeeded;
    public string? ErrorMessage => Errors.FirstOrDefault();

    public static Result Success() => new Result { Succeeded = true };
    public static Result Failure(params string[] errors) => new Result { Succeeded = false, Errors = errors.ToList() };
    public static Result Failure(IEnumerable<string> errors) => new Result { Succeeded = false, Errors = errors.ToList() };
}
