using System;
using System.Text;
using LiterCast.AudioSources;

namespace LiterCast.RadioClients
{
    internal static class IcyUtils
    {
        /*
         * 
         * 	Length - One byte, the value of this byte * 16 is the length of the rest of the message. If the title  hasn't changed since the last metadata message, this should be zero and the only byte in the message.
	     *    Message - Usually the title of the song currently playing, encoded in ASCII.
	     *    Padding - At least one zero byte must end the message and there must be enough zeros to make the part   after the length byte a multiple of 16. Thus a message of 15 bytes would have 1 zero byte following it   and a message of 16 bytes would have 16 zero bytes following it.
	     *    Let's add an example, if we wanted to send the song title "U2 - One" to the client we would add the   following data to the stream:
         *
	     *    Hex:   0x01 0x55 0x32 0x20 0x2D 0x20 0x4F 0x6E 0x65 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00
	     *    ASCII: N/A  U    2    SPC  -    SPC  O    n    e    NUL  NUL  NUL  NUL  NUL  NUL  NUL  NUL
         *
         */
        public static byte[] GetIcyMetaData(this IAudioSource source)
        {
            string metaStr = "";
            metaStr += "StreamTitle='";
            metaStr += IcyEscape(source?.Title) ?? "";
            metaStr += "';";
            metaStr += '\0';
            byte[] meta = Encoding.UTF8.GetBytes(metaStr);
            int len = meta.Length - 1;
            if(len % 16 != 0)
            {
                len = ((len / 16) * 16) + 16;
            }
            byte[] finalByteArr = new byte[len + 1];
            finalByteArr[0] = Convert.ToByte(len / 16);
            Array.Copy(meta, 0, finalByteArr, 1, meta.Length);
            return finalByteArr;
        }

        private static string IcyEscape(string title)
        {
            return title.Replace("'", "'", StringComparison.Ordinal).Replace("\"", "\"", StringComparison.Ordinal);
        }
    }
}
