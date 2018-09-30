using System.IO;

namespace LiterCast.RadioClients
{
    public interface IRadioClient
    {
        Stream OutputStream { get; }
    }
}