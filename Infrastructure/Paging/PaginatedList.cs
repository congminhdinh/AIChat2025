namespace Infrastructure.Paging;

public class PaginatedList<T>
{
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }
    //public int TotalItems { get; private set; }
    public int PageSize { get; private set; }
    public List<T> Items { get; private set; }

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        //TotalItems = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
        PageSize = pageSize;
    }


}
public interface IPaginatedRequest
{
    int pageIndex { get; set; }
    int pageSize { get; set; }
    string? sortBy { get; set; }
}

public class PaginatedRequest : BaseRequest, IPaginatedRequest
{
    public string? sortBy { get; set; }
    public int pageIndex { get; set; } = 1;
    public int pageSize { get; set; } = 20;
}
