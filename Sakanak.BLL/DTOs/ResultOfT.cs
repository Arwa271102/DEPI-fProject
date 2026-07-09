namespace Sakanak.BLL.DTOs;

public class Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> Success(T data) => new Result<T>
    {
        Succeeded = true,
        Data = data
    };

    public new static Result<T> Failure(params string[] errors) => new Result<T>
    {
        Succeeded = false,
        Errors = errors.ToList()
    };

    public new static Result<T> Failure(IEnumerable<string> errors) => new Result<T>
    {
        Succeeded = false,
        Errors = errors.ToList()
    };
}
