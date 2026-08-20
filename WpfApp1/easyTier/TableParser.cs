using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPL_WpfApp.easyTier
{
    public static class TableParser
    {
        /// <summary>
        /// 解析 ASCII 表格格式的字符串，返回 NetworkNode 对象数组
        /// </summary>
        /// <param name="input">表格文本</param>
        /// <returns>NetworkNode 数组</returns>
        public static NetworkNode[] ParseTable(string input)
        {
            var nodes = new List<NetworkNode>();

            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                             .Select(x => x.Trim())
                             .Where(x => x.StartsWith("|") || x.StartsWith("│"))
                             .Where(x => !x.StartsWith("|-") && !x.StartsWith("├") && !x.StartsWith("└"))
                             .ToArray();

            if (lines.Length == 0) throw new ArgumentException("输入为空或格式不正确");

            string[] headers = SplitLine(lines[0]);
            int ipv4Idx = FindHeader(headers, "ipv4");
            int hostnameIdx = FindHeader(headers, "hostname");
            int costIdx = FindHeader(headers, "cost");
            int latMsIdx = FindHeader(headers, "lat(ms)", "lat_ms");
            int lossRateIdx = FindHeader(headers, "loss_rate");
            int rxBytesIdx = FindHeader(headers, "rx", "rx_bytes");
            int txBytesIdx = FindHeader(headers, "tx", "tx_bytes");
            int tunnelProtoIdx = FindHeader(headers, "tunnel", "tunnel_proto");
            int natTypeIdx = FindHeader(headers, "nat", "nat_type");
            int idIdx = FindHeader(headers, "id");
            int versionIdx = FindHeader(headers, "version");

            if (ipv4Idx == -1 || hostnameIdx == -1)
                throw new InvalidOperationException("表头缺少必要字段");

            // 解析数据行（跳过第一行表头）
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cells = SplitLine(lines[i]);

                if (cells.Length == 0) continue;

                string ipv4 = SafeGet(cells, ipv4Idx);
                int cidrIndex = ipv4.IndexOf('/');
                if (cidrIndex >= 0) ipv4 = ipv4.Substring(0, cidrIndex);
                string hostname = SafeGet(cells, hostnameIdx);
                if (string.IsNullOrWhiteSpace(ipv4) || string.IsNullOrWhiteSpace(hostname)) continue;

                var node = new NetworkNode
                {
                    Ipv4 = ipv4,
                    Hostname = hostname,
                    Cost = SafeGet(cells, costIdx),
                    LatMs = SafeGet(cells, latMsIdx),
                    LossRate = SafeGet(cells, lossRateIdx),
                    RxBytes = SafeGet(cells, rxBytesIdx),
                    TxBytes = SafeGet(cells, txBytesIdx),
                    TunnelProto = SafeGet(cells, tunnelProtoIdx),
                    NatType = SafeGet(cells, natTypeIdx),
                    Id = SafeGet(cells, idIdx),
                    Version = SafeGet(cells, versionIdx)
                };

                nodes.Add(node);
            }

            return nodes.ToArray();
        }
        private static string[] SplitLine(string line)
        {
            char separator = line.IndexOf('│') >= 0 ? '│' : '|';
            var parts = line.Split(separator).Select(x => x.Trim()).ToList();
            if (parts.Count > 0 && parts[0].Length == 0) parts.RemoveAt(0);
            if (parts.Count > 0 && parts[parts.Count - 1].Length == 0) parts.RemoveAt(parts.Count - 1);
            return parts.ToArray();
        }

        private static int FindHeader(string[] headers, params string[] names)
        {
            return Array.FindIndex(headers, header =>
                names.Any(name => string.Equals(header, name, StringComparison.OrdinalIgnoreCase)));
        }

        // 安全获取，越界返回空字符串
        private static string SafeGet(string[] array, int index)
        {
            return index >= 0 && index < array.Length ? array[index] : "";
        }
        //private static string ExtractCell(string line, int start, int end)
        //{
        //    if (start >= line.Length) return "";
        //    int actualEnd = Math.Min(end, line.Length);
        //    return line.Substring(start, actualEnd - start).Trim();
        //}
        
    }
    public class NetworkNode
    {
        public string Ipv4 { get; set; }
        public string Hostname { get; set; }
        public string Cost { get; set; }
        public string LatMs { get; set; }
        public string LossRate { get; set; }
        public string RxBytes { get; set; }
        public string TxBytes { get; set; }
        public string TunnelProto { get; set; }
        public string NatType { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }

        public override string ToString()
        {
            return string.Format("IPv4: {0}, Hostname: {1}, Cost: {2}, Latency: {3} ms, NAT: {4}, ID: {5}",
                Ipv4, Hostname, Cost, LatMs, NatType, Id);
        }
    }
}
