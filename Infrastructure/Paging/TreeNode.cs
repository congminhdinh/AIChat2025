namespace Infrastructure.Paging;

public class TreeNode<T>
{
    public TreeNode(T data, T? parent)
    {
        this.parent = parent;
        this.data = data;
        children = new List<TreeNode<T>>();
    }

    public T? parent { get; set; }
    public T data { get; set; }
    public List<TreeNode<T>> children { get; set; } = new List<TreeNode<T>>();
}
public class TreeDropdown
{
    public TreeDropdown(int data, string label)
    {
        this.data = data;
        this.label = label;
        children = new List<TreeDropdown>();
    }

    public int data { get; set; }
    public string label { get; set; }
    public List<TreeDropdown>? children { get; set; }
}