﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Steg
{
    class LSBFunctions
    {
        static Bitmap bmp;
        static byte[] rgbValues;
        static BitmapData bmpData;
        static int bytes;
        static IntPtr ptr;


        /*
        /////////////////////////////////////////////////
        Functions for reading and writing the LSB of imgs
        /////////////////////////////////////////////////
        */

        public static void writeLSB(string filename, string outputDir, int bitCount, string message = null, byte[] byteInput = null, bool endMarker = false)
        {
            openImg(filename);
            byte[] byteMsg = null;

            // Based on whether the input is via file or direct text, assign the byteMsg array
            if (message != null)
            {
                byteMsg = new byte[message.Length * sizeof(char)];
                Buffer.BlockCopy(message.ToCharArray(), 0, byteMsg, 0, byteMsg.Length);
            }
            else
            {
                List<byte> tempBytes = byteInput.ToList();
                tempBytes.AddRange(new List<byte>() { 0x4c, 0x53, 0x42 });
                byteMsg = tempBytes.ToArray();
            }

            BitArray bitMsg = new BitArray(byteMsg);
            //
            Action writeLoop = delegate
            {
                // Cycle though the bits, then the 
                for (int j = 1; j <= bitCount; j++)
                {
                    for (int i = 0; i < rgbValues.Length; i++)
                    {
                        // Test to see if we've written all of bitMsg
                        if (i * j < bitMsg.Length)
                        {
                            // If the intended message is different from the preexisting bit, write to it.
                            if (bitMsg[i * j] ^ Convert.ToBoolean(rgbValues[i] & j))
                            {
                                //(n & ~1) | b
                                rgbValues[i] = (byte)((rgbValues[i] & ~j) | Convert.ToInt32(bitMsg[i * j]));
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            };
            writeLoop();
            closeImg();

            // Save the modified image.
            bmp.Save(LSBForm.initialPath + "\\output.png");
            bmp.Dispose();
        }

        public static void readLSB(string filename, int bitCount, bool concat, bool fileout, bool cut, bool trim)
        {

            openImg(filename);

            BitArray message = new BitArray(rgbValues.Length * bitCount);
            byte[] messageBytes = new byte[message.Length / 8];

            for (int j = 0; j < bitCount; j++)
            {
                for (int i = 0; i < rgbValues.Length; i++)
                {
                    // Add the LSB to bitArray
                    message[i + (j * rgbValues.Length)] = (rgbValues[i] & (1 << j)) == 1;
                }
                //System.Windows.Forms.MessageBox.Show(j + "");
            }

            closeImg();
            bmp.Dispose();

            // Copy the bits from the image into the byte[]
            message.CopyTo(messageBytes, 0);

            DisplayOutput dispOutput = null;

            if (fileout)
            {
                dispOutput = new DisplayOutput(null, messageBytes, trim, cut);
            }
            else
            {
                // Copy the byte[] into a char[] and into a string
                char[] chars = new char[messageBytes.Length / sizeof(char)];
                Buffer.BlockCopy(messageBytes, 0, chars, 0, messageBytes.Length);
                string str = new string(chars);

                // Cut the gibberish if the user wants you to.
                if (concat)
                {
                    String tmp = "";
                    foreach (char c in str)
                    {
                        // Check if each character is in the desired ascii range
                        if (c >= 0x20 && c <= 0x7F)
                        {
                            tmp += c;
                        }
                    }
                    str = tmp;
                }

                // Show the message
                dispOutput = new DisplayOutput(str, null, false, cut);
            }
            dispOutput.Show();
        }



        /*
        /////////////////////////////////////////////////
        Functions for opening and closing images
        /////////////////////////////////////////////////
        */

        public static void openImg(string filename)
        {
            if (File.Exists(filename))
            {
                bmp = new Bitmap(filename);
                // Lock the bitmap's bits.  
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

                // Get the address of the first line.
                ptr = bmpData.Scan0;

                bytes = Math.Abs(bmpData.Stride) * bmp.Height;
                rgbValues = new byte[bytes];

                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            }
        }

        public static void closeImg()
        {
            // Copy the RGB values back to the bitmap & unlock
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            bmp.UnlockBits(bmpData);
        }
    }
}
