﻿using ShotgunMetagenome.External;
using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Several;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WfComponent.Utils.FileUtils;


namespace ShotgunMetagenome.Utils
{
	// https://gist.github.com/ForeverZer0/a2cd292bd2f3b5e114956c00bb6e872b#file-extracttargz-cs-L11
	public class TarGzFileUtils
    {

		/// <summary>
		/// Extracts a <i>.tar.gz</i> archive to the specified directory.
		/// </summary>
		/// <param name="filename">The <i>.tar.gz</i> to decompress and extract.</param>
		/// <param name="outputDir">Output directory to write the files.</param>
		public static void ExtractTarGz(string filename, string outputDir)
		{
			using (var stream = File.OpenRead(filename))
				ExtractTarGz(stream, outputDir);
		}

		/// <summary>
		/// Extracts a <i>.tar.gz</i> archive stream to the specified directory.
		/// </summary>
		/// <param name="stream">The <i>.tar.gz</i> to decompress and extract.</param>
		/// <param name="outputDir">Output directory to write the files.</param>
		public static void ExtractTarGz(Stream stream, string outputDir)
		{
			// A GZipStream is not seekable, so copy it first to a MemoryStream
			using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
			{
				// const int chunk = 4096;
				const int chunk = 2 * 1024 * 1024; //2MB var fbuf = new byte[chunk];

				using (var memStr = new MemoryStream())
				{
					int read;
					var buffer = new byte[chunk];
					do
					{
						read = gzip.Read(buffer, 0, chunk);
						memStr.Write(buffer, 0, read);
					} while (read == chunk);
					// while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
					// {
					// memStr.Write(buffer, 0, read);
					// }

					memStr.Seek(0, SeekOrigin.Begin);
					ExtractTar(memStr, outputDir);
				}
			}
		}

		/// <summary>
		/// Extractes a <c>tar</c> archive to the specified directory.
		/// </summary>
		/// <param name="filename">The <i>.tar</i> to extract.</param>
		/// <param name="outputDir">Output directory to write the files.</param>
		public static void ExtractTar(string filename, string outputDir)
		{
			using (var stream = File.OpenRead(filename))
				ExtractTar(stream, outputDir);
		}

		/// <summary>
		/// Extractes a <c>tar</c> archive to the specified directory.
		/// </summary>
		/// <param name="stream">The <i>.tar</i> to extract.</param>
		/// <param name="outputDir">Output directory to write the files.</param>
		public static void ExtractTar(Stream stream, string outputDir)
		{
			var buffer = new byte[100];
			while (true)
			{
				stream.Read(buffer, 0, 100);
				var name = Encoding.ASCII.GetString(buffer).Trim('\0');
				if (String.IsNullOrWhiteSpace(name))
					break;
				stream.Seek(24, SeekOrigin.Current);
				stream.Read(buffer, 0, 12);
				var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);

				stream.Seek(376L, SeekOrigin.Current);

				var output = Path.Combine(outputDir, name);
				if (!Directory.Exists(Path.GetDirectoryName(output)))
					Directory.CreateDirectory(Path.GetDirectoryName(output));
				//if (!name.Equals("./", StringComparison.InvariantCulture))
				if (!name.EndsWith("/"))
				{
					using (var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
					{
						var buf = new byte[size];
						stream.Read(buf, 0, buf.Length);
						str.Write(buf, 0, buf.Length);
					}
				}

				var pos = stream.Position;

				var offset = 512 - (pos % 512);
				if (offset == 512)
					offset = 0;

				stream.Seek(offset, SeekOrigin.Current);
			}
		}


		public static string UnTarGzCmd(string filename, string outputDir, IProgress<string> progress = null)
		{
			if (progress == null)
				progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

			var stdout = string.Empty;
			var stderr = string.Empty;

			var args = " -xzf " + GetDoubleQuotationPath( filename );
			var proc = RequestCommand.GetInstance();
			var commandRes = proc.ExecuteWinCommand("tar ", args, ref stdout, ref stderr, outputDir);

			if (commandRes)
				progress.Report("response " + commandRes);

			return string.Empty;
		}

		public static string UnZipFile(string zipFile, IProgress<string> progress = null)
        {
			if (progress == null)
				progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

			using (var zip = ZipFile.OpenRead(zipFile))
			{
				var unzipDir = Path.GetDirectoryName(zipFile);
				foreach (var entry in zip.Entries)
				{

					var cdir = Path.Combine(
											unzipDir,
											Path.GetDirectoryName(entry.FullName));

					if (!Directory.Exists(cdir))
						Directory.CreateDirectory(cdir);

					// 以下の行を修正
					string destPath = Path.GetFullPath(Path.Combine(unzipDir, entry.FullName));
					// 以下の4行を追加
					if (!destPath.StartsWith(unzipDir))
					{
						progress.Report("Malicious entry has detected. ");
						return "Malicious entry has detected.";
					}
					entry.ExtractToFile(destPath, true);    // ファイルを上書きする
				}
			}
			return string.Empty;
        }
	}
}
