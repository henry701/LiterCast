using System.IO;

namespace LiterCast
{
    public interface IRadioClient
    {
        Stream OutputStream { get; }
    }
}