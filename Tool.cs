using System;

public  class Tool
{
	public static byte[] string2ByteArray(String str)
    {
        String[] sa = str.Split(" ");
        byte[] result = new byte[sa.Length];
        for (int i = 0; i < sa.Length; i++)
        {
            result[i] = Byte.Parse(sa[i], System.Globalization.NumberStyles.HexNumber);
        }
        return result;
    }
    public static String byteArray2String(byte[] arr)
    {
        String ba="";
    
        for (int i = 0; i < arr.Length; i++)
        {
            ba += arr[i].ToString("X2");
            if (i != arr.Length - 1) ba+= " ";
        }
        return ba;
    }
}
