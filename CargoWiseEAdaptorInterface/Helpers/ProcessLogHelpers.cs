using System.Xml.Linq;
using ToolsSharp;

namespace CargoWiseEAdaptorInterface.Helpers
{
	internal static class ProcessLogHelpers
	{
		public static string TryParseProcessLog(XElement? body)
		{
			if (body == null)
				return "";
			try
			{
				var str = XMLHelpers.ConvertXElementToString(body);
				var processLogStart = str.LastIndexOf("<ProcessingLog>") + "<ProcessingLog>".Length;
				var processLogEnd = str.LastIndexOf("</ProcessingLog>");
				if (processLogStart == -1 || processLogEnd == -1)
					return "";
				var processLog = str.Substring(processLogStart, processLogEnd - processLogStart);
				return processLog;
			}
			catch (Exception)
			{
				return "";
			}
		}
	}
}
