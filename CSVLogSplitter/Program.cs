using System;
using System.Text;

namespace CSVLogSplitter // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = string.Empty;
            if (args.Length == 0)
            {
                Console.WriteLine("分割するCSVファイルのパスを入力してください。");

                path = Console.ReadLine();
            }
            else
            {
                path = args[0];
            }

            if (!File.Exists(path))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);



            using (var sr = new StreamReader(path, Encoding.UTF8))
            {

                var headers = new List<string>();
                var line = string.Empty;
                do
                {
                    line = sr.ReadLine() ?? string.Empty;
                    headers.Add(line);

                }
                while (!line.StartsWith("Time (s)"));
                var columnNames = line;
                var splitNames = columnNames.Split(",", StringSplitOptions.None);


                Func<string, int> f = (name) =>
                {
                    return splitNames.Select((item, index) => (item, index))
                    .Where(pare => string.Compare(pare.item, name, true) == 0)
                    .FirstOrDefault().index;
                };

                var timeIndex = f("Time (s)");

                var latIndex = f("Latitude (deg)");

                var longIndex = f("Longitude (deg)");

                var vIndex = f("Speed (m/s)");

                Func<string, double> getSpeed = (str) =>
                {
                    var splitLine = str.Split(",", StringSplitOptions.None);
                    double tempSpeed = double.NaN;
                    double.TryParse(splitLine[vIndex], out tempSpeed);
                    return tempSpeed;

                };


                Func<string, double> getTime = (str) =>
                {
                    var splitLine = str.Split(",", StringSplitOptions.None);

                    var temptime = 0.0;
                    double.TryParse(splitLine[timeIndex], out temptime);
                    return  temptime;

                };


                while (!sr.EndOfStream)
                {
                    var lines = new List<string>();
                    double nowSpeed = 0.0;

                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine() ?? string.Empty;
                        if(string.IsNullOrEmpty(line))
                        {
                            continue;
                        }
                        lines.Add(line);
                        nowSpeed = getSpeed(line);
                        
                        if (nowSpeed > 5.4)
                        {
                            var carStartIndex = lines.FindLastIndex(x => getSpeed(x) < 0.5);
                            var carStartTime = getTime(lines[carStartIndex]);
                            var startIndex = lines.FindLastIndex(x => getTime(x) < carStartTime - 10);
                            if (startIndex > 0)
                            {
                                lines.RemoveRange(0, startIndex - 1);
                            }

                            break;
                        }
                        var nowTime = getTime(line);
                        if (lines.Any() && nowTime - getTime(lines.First()) > 20.0)
                        {
                            var delStartIndex = lines.FindIndex(x => getTime(line) - getTime(x) < 20.0);
                            lines.RemoveRange(0, delStartIndex);
                        }

                    }


                    if(!lines.Any(x => getSpeed(x) > 5.4))
                    {
                        break;
                    }

                    double firstStop = 0.0;
                    while (!sr.EndOfStream)
                    {

                        line = sr.ReadLine() ?? string.Empty;
                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }
                        lines.Add(line);
                        nowSpeed = getSpeed(line);

                        if (nowSpeed > 2.7)
                        {
                            firstStop = 0.0;
                        }
                        if (nowSpeed < 1.0 && firstStop == 0.0)
                        {
                            firstStop = getTime(line);
                        }
                        if (nowSpeed < 1.0 && firstStop != 0.0 && getTime(line) - firstStop > 10.0)
                        {
                            break;
                        }

                    } 


                    var time_ms = getTime(lines.First());
                    var dto = DateTimeOffset.FromUnixTimeMilliseconds((long)(time_ms * 1000)).LocalDateTime;
                    var tempDate = new DateTime(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, dto.Millisecond);
                    string newFileName = $"{fileName}_{tempDate:yyyyMMddHHmmss}{extension}";
                    string newFilePath = Path.Combine(directory, newFileName);
                    using (var sw = new StreamWriter(newFilePath, false, Encoding.UTF8))
                    {
                        foreach (var header in headers)
                        {
                            sw.WriteLine(header);
                        }
                        foreach(var record in lines)
                        {
                            sw.WriteLine(record);
                        }
                        sw.Flush();
                        sw.Close();
                    }
                }
            }
        }
    }
}