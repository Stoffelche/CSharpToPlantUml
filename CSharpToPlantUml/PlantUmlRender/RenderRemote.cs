using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToPlantUml.PlantUmlRender {
	/// <summary>
	/// Code is based on the remote rendering in https://github.com/KevReed/PlantUml.Net
	/// </summary>
	public static class RenderRemote {

		static Uri GetRenderUrl(string remoteUrl, string code, EOutputFormat outputFormat) {
			string urlComponent = TextEncoding.EncodeUrl(code);
			return DoGetRenderUrl(remoteUrl, urlComponent, outputFormat);
		}
		static Uri DoGetRenderUrl(string remoteUrl, string urlComponent, EOutputFormat outputFormat) {
			var baseUri = new Uri(remoteUrl);
			return AppendPath(baseUri, outputFormat.ToString().ToLowerInvariant(), urlComponent);
		}
		private static Uri AppendPath(Uri uri, params string[] paths) {
			return new Uri(paths.Aggregate(uri.AbsoluteUri, (current, path) => $"{current.TrimEnd('/', '\\')}/{path.TrimStart('/')}"));
		}
		public static async Task<byte[]> RenderAsync(string remoteUrl, string code, EOutputFormat outputFormat, CancellationToken cancellationToken = default) {
			var renderUrl = GetRenderUrl(remoteUrl, code, outputFormat);

			using (HttpClient httpClient = new HttpClient()) {
				var result = await httpClient.GetAsync(renderUrl, cancellationToken).ConfigureAwait(false);

				if (result.IsSuccessStatusCode) {
					return await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
				}

				if (result.StatusCode == HttpStatusCode.BadRequest) {
					var messages = result.Headers.GetValues("X-PlantUML-Diagram-Error");
					throw new RenderingException(code, string.Join(Environment.NewLine, messages));
				}

				throw new HttpRequestException(result.ReasonPhrase);
			}
		}
		public static void RenderFile(string remoteUrl, string filePath, EOutputFormat outputFormat, string outputDir = null) {
			FileInfo file = new FileInfo(filePath);
			string code;
			using (StreamReader reader = new StreamReader(file.FullName)) {
				code = reader.ReadToEnd();
				//StringBuilder sb = new StringBuilder();
				//while (!reader.EndOfStream) {
				//	sb.AppendLine(reader.ReadLine());
				//}
				//string code = sb.ToString();
			}
			var bytes = RenderAsync(remoteUrl, code, outputFormat).Result;
			string destFile;
			if(string.IsNullOrEmpty(outputDir)) {
				destFile = file.FullName.Substring(0, file.FullName.Length - file.Extension.Length) + "." + outputFormat.ToString().ToLowerInvariant();
			} else {
				destFile =  Path.Combine(outputDir, file.Name.Substring(0, file.Name.Length - file.Extension.Length) + "." + outputFormat.ToString().ToLowerInvariant());
			}
			File.WriteAllBytes(destFile, bytes);
		}
	}
}
