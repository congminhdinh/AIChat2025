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
    int PageIndex { get; set; }
    int PageSize { get; set; }
}

public class PaginatedRequest : BaseRequest, IPaginatedRequest
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
