namespace FileArchive.Utils;

public class ResultObject<T>
{
    public bool Success { get; set; } = true;
    public List<string> Messages { get; set; } = [];
    public T? Result { get; set; }
}
