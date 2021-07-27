﻿using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dyn_nft_loader
{
    class Program
    {

        static HttpWebRequest webRequest;


        static void Main(string[] args)
        {

            Global.LoadSettings();

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
            string ownerAddress = "dy1qhtg9zqf2fdh07vahzap3rtrg27va9kvmex7yz4";

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

            string hexNFTRawData = ByteToHex(nftRawData);

            byte[] hash = hasher.ComputeHash(nftRawData);

            string strHash = ByteToHex(hash);
            string nftCommand = "00" + strHash;     //add asset class opcode

            string rpcAddAssetClass = "{ \"id\": 0, \"method\" : \"sendtoaddress\", \"params\" : [ \"" + ownerAddress +"\" , 0.001, \"\", \"\", true ], \"nft_command\" : \"" + nftCommand + "\"  }";

            string rpcResult = rpcExec(rpcAddAssetClass);
            dynamic jRPCResult = JObject.Parse(rpcResult);
            string txID = jRPCResult.result;

            byte[] byteOwnerAddr = System.Text.Encoding.UTF8.GetBytes(ownerAddress);
            byte[] byteTXID = HexToByte(txID);

            hasher.Initialize();
            byte[] bHash1Buffer = new byte[hash.Length + byteOwnerAddr.Length];
            System.Buffer.BlockCopy(hash, 0, bHash1Buffer, 0, hash.Length);
            System.Buffer.BlockCopy(byteOwnerAddr, 0, bHash1Buffer, hash.Length, byteOwnerAddr.Length);
            byte[] hash1 = hasher.ComputeHash(bHash1Buffer);

            hasher.Initialize();
            byte[] bHash2Buffer = new byte[hash1.Length + byteTXID.Length];
            System.Buffer.BlockCopy(hash1, 0, bHash2Buffer, 0, hash1.Length);
            System.Buffer.BlockCopy(byteTXID, 0, bHash2Buffer, hash1.Length, byteTXID.Length);
            byte[] hash2 = hasher.ComputeHash(bHash2Buffer);

            string nftHash = ByteToHex(hash2);


            string command = "add-class";

            rpcAddAssetClass = "{ \"id\": 0, \"method\" : \"submitnft\", \"params\" : [ \"" + command + "\", \"" + hexNFTRawData + "\", \"" + ownerAddress +"\", \"" + txID + "\", \"\" ] }";

            string nftHashVerify = rpcExec(rpcAddAssetClass);

        }


        public static byte[] HexToByte(string data)
        {
            data = data.ToUpper();
            byte[] result = new byte[data.Length / 2];
            for ( int i = 0; i < data.Length; i += 2)
            {
                byte hi = hex(data[i]);
                byte lo = hex(data[i + 1]);
                result[i / 2] = (byte)(hi * 16 + lo);
            }

            return result;
        }

        public static byte hex(char data)
        {

            if (data < 'A')
                return (byte)(data - '0');
            else
                return (byte)((data - 'A' ) + 10);
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


        public static string rpcExec(string command)
        {
            webRequest = (HttpWebRequest)WebRequest.Create(Global.FullNodeRPC());
            webRequest.KeepAlive = false;

            var data = Encoding.ASCII.GetBytes(command);

            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = data.Length;

            var username = Global.FullNodeUser();
            var password = Global.FullNodePass();
            string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            webRequest.Headers.Add("Authorization", "Basic " + encoded);


            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }


            var webresponse = (HttpWebResponse)webRequest.GetResponse();

            string submitResponse = new StreamReader(webresponse.GetResponseStream()).ReadToEnd();

            webresponse.Dispose();


            return submitResponse;
        }



    }
}
