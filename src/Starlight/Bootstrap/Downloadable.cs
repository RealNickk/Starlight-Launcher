﻿using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Starlight.Misc;
using Starlight.Misc.Extensions;

namespace Starlight.Bootstrap;

/// <summary>
///     Represents a downloadable file.
/// </summary>
public partial class Downloadable
{
    internal Downloadable(string versionHash, string name, string checksum, long size, long trueSize)
    {
        VersionHash = versionHash;
        Name = name;
        Checksum = checksum;
        TrueSize = trueSize;
        Size = size;
    }

    /// <summary>
    ///     <para>The MD5 checksum of the file.</para>
    ///     For more information on MD5:<br/>
    ///     <see href="https://en.wikipedia.org/wiki/MD5"/>
    /// </summary>
    public readonly string Checksum;

    /// <summary>
    ///     The name of the file.
    /// </summary>
    public readonly string Name;

    /// <summary>
    ///     The compressed size of the file.
    /// </summary>
    public readonly long Size;

    /// <summary>
    ///     The uncompressed size of the file.
    /// </summary>
    public readonly long TrueSize;

    /// <summary>
    ///     The version hash the file is categorized under.
    /// </summary>
    public readonly string VersionHash;

    /// <summary>
    ///    Download the file to the specified directory.
    /// </summary>
    /// <exception cref="TaskCanceledException"/>
    public async Task DownloadAsync(string dir, CancellationToken token = default)
    {
        var filePath = Path.Combine(dir, Name);

        using (var fileStm = File.OpenWrite(filePath))
        {
            var req = new RestRequest($"version-{VersionHash}-{Name}")
            {
                ResponseWriter = stm =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    stm.CopyTo(fileStm);
                    return stm;
                }
            };
            await Bootstrapper.RbxCdnClient.DownloadDataAsync(req, token);
        }

        if (!Validate(filePath))
        {
            File.Delete(filePath);
            throw new NotImplementedException();
        }
    }
    
    internal bool Validate(string filePath)
    {
        if (!File.Exists(filePath) || new FileInfo(filePath).Length != Size)
            return false;

        using var stm = File.OpenRead(filePath);
        using var hasher = MD5.Create();
        var hash = BitConverter.ToString(hasher.ComputeHash(stm)).Replace("-", "").ToLower();

        return hash == Checksum;
    }
}