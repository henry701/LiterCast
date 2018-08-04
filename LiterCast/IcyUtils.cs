using System;
using System.Collections.Generic;
using System.Text;

namespace LiterCast
{
    internal static class IcyUtils
    {
        public static byte[] GetIcyMetaData(this IAudioSource source)
        {
            byte len = 0;
            string metaStr = "";
            metaStr += "StreamTitle='";
            metaStr += source.Title;
            metaStr += "'";
            byte[] meta = Encoding.UTF8.GetBytes(metaStr);
            byte[] finalByteArr = new byte[meta.Length + 1];
            len = Convert.ToByte(meta.Length / 16M);
            finalByteArr[0] = len;
            Array.Copy(meta, 0, finalByteArr, 1, meta.Length);
            return finalByteArr;
        }
    }
}
