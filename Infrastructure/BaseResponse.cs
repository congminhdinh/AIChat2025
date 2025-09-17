using Infrastructure.Utils;

namespace Infrastructure
{
    public class BaseResponse: BaseMessage
    {
        public BaseResponse()
        {

        }
        public BaseResponse(string message, BaseResponseStatus status, Guid correlationId)
        {
            Status = status;
            Message = message;
            _correlationId = correlationId;
        }
        public BaseResponseStatus Status { get; set; } = BaseResponseStatus.Success;
        public string Message { get; set; } = "";
        public override string ToString()
        {
            return this.ToJson();
        }
    }

    public class BaseResponse<T> : BaseResponse
    {
        public BaseResponse()
        {

        }
        public BaseResponse(string message, BaseResponseStatus status, Guid correlationId) : base(message, status, correlationId)
        {
        }
        public BaseResponse(T? data, Guid correlationId)
        {
            Data = data;
            Status = BaseResponseStatus.Success;
            _correlationId = correlationId;

        }
        public T? Data { get; set; }
    }

    public class NumberResponseData
    {
        public NumberResponseData(int value)
        {
            Value = value;
        }
        public int Value { get; set; }
    }

    public enum BaseResponseStatus
    {
        Error,
        Success
    }
    public class DropDownDto
    {
        public DropDownDto(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; set; }
        public string Name { get; set; }
    }
}
