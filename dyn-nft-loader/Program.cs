using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace dyn_nft_loader
{
    class Program
    {

        static void Main(string[] args)
        {

            /*
            1 - create asset class metadata and generate hash
            2 - submit hash for mining into blockchain, get txid
            3 - once mined, submit nft data command to local fullnode
             */


            /*
            NFT data format:
                first byte is command
                00 - add asset class
                01 - add asset

                two bytes metadata length

                metadata  <variable>

                max count <8 bytes>

                (for asset only)
                three bytes data length

                binary data  <variable>

                serial number <8 bytes>

            */

            string assetClassMetaData = "The only way whereby anyone divests himself of his natural liberty, and puts on the bonds of civil society, is by agreeing with other men to join and unite into a community, for their comfortable, safe, and peaceable living one amongst another, in a secure enjoyment of their properties, and a greater security against any that are not of it.";
            //string ownerAddress = "dy1qzvx3yfrucqa2ntsw8e7dyzv6u6dl2c2wjvx5jy";
            int metaDataLen = assetClassMetaData.Length;

            SHA256 hasher = SHA256.Create();

            long nftHashLen = metaDataLen + 2 + 8;     //2 bytes for length of metadata, 8 bytes for 64 bit serial
            byte[] nftRawData = new byte[nftHashLen];

            nftRawData[0] = (byte)(nftHashLen >> 8);
            nftRawData[1] = (byte)(nftHashLen & 0xFF);

            byte[] metaDataBytes = System.Text.Encoding.UTF8.GetBytes(assetClassMetaData);
            for (int i = 0; i < metaDataLen; i++)
                nftRawData[i + 2] = metaDataBytes[i];

            UInt64 maxSerial = 1;
            nftRawData[metaDataLen +  2] = (byte)(maxSerial >> 56);
            nftRawData[metaDataLen + 2+1] = (byte)((maxSerial & 0x00FF000000000000) >> 48);
            nftRawData[metaDataLen +  2+2] = (byte)((maxSerial & 0x0000FF0000000000) >> 40);
            nftRawData[metaDataLen +  2+3] = (byte)((maxSerial & 0x000000FF00000000) >> 32);
            nftRawData[metaDataLen +  2+4] = (byte)((maxSerial & 0x00000000FF000000) >> 24);
            nftRawData[metaDataLen +  2+5] = (byte)((maxSerial & 0x0000000000FF0000) >> 16);
            nftRawData[metaDataLen +  2+6] = (byte)((maxSerial & 0x000000000000FF00) >> 8);
            nftRawData[metaDataLen +  2+7] = (byte)((maxSerial & 0x00000000000000FF));

            byte[] hash = hasher.ComputeHash(nftRawData);

            string strHash = ByteToHex(hash);

            string rpcAddAssetClass = "{ \"id\": 0, \"method\" : \"sendtoaddress\", \"params\" : [ \"dy1q6y6uv9thwl99up2l4pj9q3l4lfuwml6wn5863q\" , 0], \"nft_command\" : \"\"  }";



        }


        public static string ByteToHex (byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }
    }
}
