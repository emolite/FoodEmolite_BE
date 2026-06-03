namespace FoodEmolite.Shared.Responses;

public class BaseResponse<T>
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; }

    public T? Data { get; set; }

    public static BaseResponse<T> Success(
        T data,
        string message = "Success")
    {
        return new BaseResponse<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    public static BaseResponse<T> Fail(
        string message)
    {
        return new BaseResponse<T>
        {
            IsSuccess = false,
            Message = message
        };
    }
}