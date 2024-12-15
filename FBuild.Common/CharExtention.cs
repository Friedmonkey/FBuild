﻿using System;
using System.Text;

namespace FBuild.Common;

public static class CharExtention
{
    public static bool IsEnter(this char character)
    {
        return character is '\n' or '\r';
    }
    public static bool IsVarible(this char character)
    {
        return char.IsLetter(character) || character == '_';
    }
    public static bool IsHexDigit(this char chr)
    {
        return (chr >= '0' && chr <= '9') || (chr >= 'A' && chr <= 'F') || (chr >= 'a' && chr <= 'f');
    }
    public static byte[] ToByteArray(this int number)
    {
        if (number < 0)
            throw new ArgumentOutOfRangeException(nameof(number), "Negative numbers are not supported!");

        if (number <= byte.MaxValue) // Fits in 1 byte (0-255)
            return new byte[] { (byte)(number & 0xFF) };

        if (number <= 0xFFFF) // Fits in 2 bytes (0-65535)
            return new byte[] { (byte)(number & 0xFF), (byte)((number >> 8) & 0xFF) };

        if (number <= 0xFFFFFF) // Fits in 3 bytes (0-16,777,215)
            return new byte[] { (byte)(number & 0xFF), (byte)((number >> 8) & 0xFF), (byte)((number >> 16) & 0xFF) };

        if (number <= 0x7FFFFFFF) // Fits in 4 bytes (0-2,147,483,647)
            return new byte[] { (byte)(number & 0xFF), (byte)((number >> 8) & 0xFF), (byte)((number >> 16) & 0xFF), (byte)((number >> 24) & 0xFF) };

        throw new OverflowException($"Number {number} is too large to fit in 4 bytes.");
    }
    public static Size GetAmountOfBytesNeeded(this UInt64 number)
    {
        if (number <= byte.MaxValue) // Fits in 1 byte (0-255)
            return Size.One;

        if (number <= ushort.MaxValue) // Fits in 2 bytes (0-65535)
            return Size.Two;

        if (number <= uint.MaxValue) // Fits in 4 bytes (0-4,294,967,295)
            return Size.Four;

        return Size.Eight; // Fits in 8 bytes (0-18,446,744,073,709,551,615)
    }

    public static byte[] ToByteArrayWithNegative(this int number)
    {
        // 1 byte: Range -128 to 127
        if (number >= sbyte.MinValue && number <= sbyte.MaxValue)
            return new byte[] { (byte)(number & 0xFF) };

        // 2 bytes: Range -32,768 to 32,767
        if (number >= short.MinValue && number <= short.MaxValue)
            return new byte[] { (byte)(number & 0xFF), (byte)((number >> 8) & 0xFF) };

        // 3 bytes: Range -8,388,608 to 8,388,607
        if (number >= -0x800000 && number <= 0x7FFFFF)
            return new byte[] { (byte)(number & 0xFF), (byte)((number >> 8) & 0xFF), (byte)((number >> 16) & 0xFF) };

        // 4 bytes: Range -2,147,483,648 to 2,147,483,647
        return new byte[] {
            (byte)(number & 0xFF),
            (byte)((number >> 8) & 0xFF),
            (byte)((number >> 16) & 0xFF),
            (byte)((number >> 24) & 0xFF)
        };
    }
    public static string ToFixedByteArray(this UInt64 number, int byteCount)
    {
        // Ensure byteCount is valid
        if (byteCount != 1 && byteCount != 2 && byteCount != 4 && byteCount != 8)
            throw new ArgumentException("Byte count must be 1, 2, 4, or 8.", nameof(byteCount));

        // Check if the number fits within the specified size
        UInt64 maxValue = (1UL << (byteCount * 8)) - 1; // Calculate max value for the byte size
        if (number > maxValue)
            throw new ArgumentOutOfRangeException(nameof(number),
                $"Value {number} is too large to fit in {byteCount} byte(s).");

        // Allocate a fixed-size byte array
        byte[] result = new byte[byteCount];

        // Fill the byte array with the value (little-endian)
        for (int i = 0; i < byteCount; i++)
        {
            result[i] = (byte)((number >> (8 * i)) & 0xFF);
        }

        return result.ToByteString();
    }
    public static string ToByteString(this UInt32 number)
    {
        // 4 bytes: Range 0 to 4,294,967,295
        return (new byte[] {
            (byte)(number & 0xFF),
            (byte)((number >> 8) & 0xFF),
            (byte)((number >> 16) & 0xFF),
            (byte)((number >> 24) & 0xFF)
        }.ToByteString());
    }
    public static string ToByteString(this UInt64 number)
    {
        // 8 bytes: Range 0 to 18,446,744,073,709,551,615 (should be big enough right?)
        return (new byte[] {
            (byte)(number & 0xFF),
            (byte)((number >> 8) & 0xFF),
            (byte)((number >> 16) & 0xFF),
            (byte)((number >> 24) & 0xFF),

            (byte)((number >> 32) & 0xFF),
            (byte)((number >> 40) & 0xFF),
            (byte)((number >> 48) & 0xFF),
            (byte)((number >> 56) & 0xFF),
        }.ToByteString());
    }
    public static string ToByteString(this byte[] input)
    {
        StringBuilder sb = new StringBuilder();

        foreach (byte c in input)
        {
            sb.Append(c.ToString("X2")); // Converts each character to its hex representation
            sb.Append(' ');
        }

        return sb.ToString();
    }
    public static string ToByteString(this string input)
    {
        StringBuilder sb = new StringBuilder();

        foreach (char c in input)
        {
            sb.Append(((byte)c).ToString("X2")); // Converts each character to its hex representation
            sb.Append(' ');
        }

        return sb.ToString();
    }

    public static string ToByteString(this byte b)
    {
        return b.ToString("X2")+" "; // Single byte to hex
    }
}
