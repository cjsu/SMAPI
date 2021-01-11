using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI.Web.Framework.LogParsing.Models;

namespace StardewModdingAPI.Web.Framework.LogParsing
{
    /// <summary>Parses SMAPI log files.</summary>
    public class LogParser
    {
        /*********
        ** Fields
        *********/
        /// <summary>A regex pattern matching the start of a SMAPI message.</summary>
        private readonly Regex MessageHeaderPattern = new Regex(@"^\[(?<time>\d\d[:\.]\d\d[:\.]\d\d) (?<level>[a-z]+)(?: +screen_(?<screen>\d+))? +(?<modName>[^\]]+)\] ", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching SMAPI's initial platform info message.</summary>
        private readonly Regex InfoLinePattern = new Regex(@"^SMAPI (?<apiVersion>.+) with Stardew Valley (?<gameVersion>.+) on (?<os>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching SMAPI's mod folder path line.</summary>
        private readonly Regex ModPathPattern = new Regex(@"^Mods go here: (?<path>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching SMAPI's log timestamp line.</summary>
        private readonly Regex LogStartedAtPattern = new Regex(@"^Log started at (?<timestamp>.+) UTC", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching the start of SMAPI's mod list.</summary>
        private readonly Regex ModListStartPattern = new Regex(@"^Loaded \d+ mods:$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching an entry in SMAPI's mod list.</summary>
        /// <remarks>The author name and description are optional.</remarks>
        private readonly Regex ModListEntryPattern = new Regex(@"^   (?<name>.+?) (?<version>[^\s]+)(?: by (?<author>[^\|]+))?(?: \| (?<description>.+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching the start of SMAPI's content pack list.</summary>
        private readonly Regex ContentPackListStartPattern = new Regex(@"^Loaded \d+ content packs:$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching an entry in SMAPI's content pack list.</summary>
        private readonly Regex ContentPackListEntryPattern = new Regex(@"^   (?<name>.+?) (?<version>[^\s]+)(?: by (?<author>[^\|]+))? \| for (?<for>[^\|]+)(?: \| (?<description>.+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching the start of SMAPI's mod update list.</summary>
        private readonly Regex ModUpdateListStartPattern = new Regex(@"^You can update \d+ mods?:$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching an entry in SMAPI's mod update list.</summary>
        private readonly Regex ModUpdateListEntryPattern = new Regex(@"^   (?<name>.+) (?<version>[^\s]+): (?<link>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>A regex pattern matching SMAPI's update line.</summary>
        private readonly Regex SmapiUpdatePattern = new Regex(@"^You can update SMAPI to (?<version>[^\s]+): (?<link>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        /*********
        ** Public methods
        *********/
        /// <summary>Parse SMAPI log text.</summary>
        /// <param name="logText">The SMAPI log text.</param>
        public ParsedLog Parse(string logText)
        {
            try
            {
                // skip if empty
                if (string.IsNullOrWhiteSpace(logText))
                {
                    return new ParsedLog
                    {
                        IsValid = false,
                        RawText = logText,
                        Error = "The log is empty."
                    };
                }

                // init log
                ParsedLog log = new ParsedLog
                {
                    IsValid = true,
                    RawText = logText,
                    Messages = this.CollapseRepeats(this.GetMessages(logText)).ToArray()
                };

                // parse log messages
                LogModInfo smapiMod = new LogModInfo { Name = "SMAPI", Author = "Pathoschild", Description = "", Loaded = true };
                LogModInfo gameMod = new LogModInfo { Name = "game", Author = "", Description = "", Loaded = true };
                IDictionary<string, LogModInfo> mods = new Dictionary<string, LogModInfo>();
                bool inModList = false;
                bool inContentPackList = false;
                bool inModUpdateList = false;
                foreach (LogMessage message in log.Messages)
                {
                    // collect stats
                    if (message.Level == LogLevel.Error)
                    {
                        switch (message.Mod)
                        {
                            case "SMAPI":
                                smapiMod.Errors++;
                                break;

                            case "game":
                                gameMod.Errors++;
                                break;

                            default:
                                if (mods.ContainsKey(message.Mod))
                                    mods[message.Mod].Errors++;
                                break;
                        }
                    }

                    // collect SMAPI metadata
                    if (message.Mod == "SMAPI")
                    {
                        // update flags
                        inModList = inModList && message.Level == LogLevel.Info && this.ModListEntryPattern.IsMatch(message.Text);
                        inContentPackList = inContentPackList && message.Level == LogLevel.Info && this.ContentPackListEntryPattern.IsMatch(message.Text);
                        inModUpdateList = inModUpdateList && message.Level == LogLevel.Alert && this.ModUpdateListEntryPattern.IsMatch(message.Text);

                        // mod list
                        if (!inModList && message.Level == LogLevel.Info && this.ModListStartPattern.IsMatch(message.Text))
                        {
                            inModList = true;
                            message.IsStartOfSection = true;
                            message.Section = LogSection.ModsList;
                        }
                        else if (inModList)
                        {
                            Match match = this.ModListEntryPattern.Match(message.Text);
                            string name = match.Groups["name"].Value;
                            string version = match.Groups["version"].Value;
                            string author = match.Groups["author"].Value;
                            string description = match.Groups["description"].Value;
                            mods[name] = new LogModInfo { Name = name, Author = author, Version = version, Description = description, Loaded = true };

                            message.Section = LogSection.ModsList;
                        }

                        // content pack list
                        else if (!inContentPackList && message.Level == LogLevel.Info && this.ContentPackListStartPattern.IsMatch(message.Text))
                        {
                            inContentPackList = true;
                            message.IsStartOfSection = true;
                            message.Section = LogSection.ContentPackList;
                        }
                        else if (inContentPackList)
                        {
                            Match match = this.ContentPackListEntryPattern.Match(message.Text);
                            string name = match.Groups["name"].Value;
                            string version = match.Groups["version"].Value;
                            string author = match.Groups["author"].Value;
                            string description = match.Groups["description"].Value;
                            string forMod = match.Groups["for"].Value;
                            mods[name] = new LogModInfo { Name = name, Author = author, Version = version, Description = description, ContentPackFor = forMod, Loaded = true };

                            message.Section = LogSection.ContentPackList;
                        }

                        // mod update list
                        else if (!inModUpdateList && message.Level == LogLevel.Alert && this.ModUpdateListStartPattern.IsMatch(message.Text))
                        {
                            inModUpdateList = true;
                            message.IsStartOfSection = true;
                            message.Section = LogSection.ModUpdateList;
                        }
                        else if (inModUpdateList)
                        {
                            Match match = this.ModUpdateListEntryPattern.Match(message.Text);
                            string name = match.Groups["name"].Value;
                            string version = match.Groups["version"].Value;
                            string link = match.Groups["link"].Value;
                            if (mods.ContainsKey(name))
                            {
                                mods[name].UpdateLink = link;
                                mods[name].UpdateVersion = version;
                            }
                            else
                            {
                                mods[name] = new LogModInfo { Name = name, UpdateVersion = version, UpdateLink = link, Loaded = false };
                            }

                            message.Section = LogSection.ModUpdateList;
                        }

                        else if (message.Level == LogLevel.Alert && this.SmapiUpdatePattern.IsMatch(message.Text))
                        {
                            Match match = this.SmapiUpdatePattern.Match(message.Text);
                            string version = match.Groups["version"].Value;
                            string link = match.Groups["link"].Value;
                            smapiMod.UpdateVersion = version;
                            smapiMod.UpdateLink = link;
                        }

                        // platform info line
                        else if (message.Level == LogLevel.Info && this.InfoLinePattern.IsMatch(message.Text))
                        {
                            Match match = this.InfoLinePattern.Match(message.Text);
                            log.ApiVersion = match.Groups["apiVersion"].Value;
                            log.GameVersion = match.Groups["gameVersion"].Value;
                            log.OperatingSystem = match.Groups["os"].Value;
                            smapiMod.Version = log.ApiVersion;
                        }

                        // mod path line
                        else if (message.Level == LogLevel.Info && this.ModPathPattern.IsMatch(message.Text))
                        {
                            Match match = this.ModPathPattern.Match(message.Text);
                            log.ModPath = match.Groups["path"].Value;
                            int lastDelimiterPos = log.ModPath.LastIndexOfAny(new char[] { '/', '\\' });
                            log.GamePath = lastDelimiterPos >= 0
                                ? log.ModPath.Substring(0, lastDelimiterPos)
                                : log.ModPath;
                        }

                        // log UTC timestamp line
                        else if (message.Level == LogLevel.Trace && this.LogStartedAtPattern.IsMatch(message.Text))
                        {
                            Match match = this.LogStartedAtPattern.Match(message.Text);
                            log.Timestamp = DateTime.Parse(match.Groups["timestamp"].Value + "Z");
                        }
                    }
                }

                // finalize log
                gameMod.Version = log.GameVersion;
                log.Mods = new[] { gameMod, smapiMod }.Concat(mods.Values.OrderBy(p => p.Name)).ToArray();
                return log;
            }
            catch (LogParseException ex)
            {
                return new ParsedLog
                {
                    IsValid = false,
                    Error = ex.Message,
                    RawText = logText
                };
            }
            catch (Exception ex)
            {
                return new ParsedLog
                {
                    IsValid = false,
                    Error = $"Parsing the log file failed. Technical details:\n{ex}",
                    RawText = logText
                };
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Collapse consecutive repeats into the previous message.</summary>
        /// <param name="messages">The messages to filter.</param>
        private IEnumerable<LogMessage> CollapseRepeats(IEnumerable<LogMessage> messages)
        {
            LogMessage next = null;
            foreach (LogMessage message in messages)
            {
                // new message
                if (next == null)
                {
                    next = message;
                    continue;
                }

                // repeat
                if (next.Level == message.Level && next.Mod == message.Mod && next.Text == message.Text)
                {
                    next.Repeated++;
                    continue;
                }

                // non-repeat message
                yield return next;
                next = message;
            }
            yield return next;
        }

        /// <summary>Split a SMAPI log into individual log messages.</summary>
        /// <param name="logText">The SMAPI log text.</param>
        /// <exception cref="LogParseException">The log text can't be parsed successfully.</exception>
        private IEnumerable<LogMessage> GetMessages(string logText)
        {
            LogMessageBuilder builder = new LogMessageBuilder();
            using StringReader reader = new StringReader(logText);
            while (true)
            {
                // read line
                string line = reader.ReadLine();
                if (line == null)
                    break;

                // match header
                Match header = this.MessageHeaderPattern.Match(line);
                bool isNewMessage = header.Success;

                // start/continue message
                if (isNewMessage)
                {
                    if (builder.Started)
                    {
                        yield return builder.Build();
                        builder.Clear();
                    }

                    var screenGroup = header.Groups["screen"];
                    builder.Start(
                        time: header.Groups["time"].Value,
                        level: Enum.Parse<LogLevel>(header.Groups["level"].Value, ignoreCase: true),
                        screenId: screenGroup.Success ? int.Parse(screenGroup.Value) : 0, // main player is always screen ID 0
                        mod: header.Groups["modName"].Value,
                        text: line.Substring(header.Length)
                    );
                }
                else
                {
                    if (!builder.Started)
                        throw new LogParseException("Found a log message with no SMAPI metadata. Is this a SMAPI log file?");

                    builder.AddLine(line);
                }
            }

            // end last message
            if (builder.Started)
                yield return builder.Build();
        }
    }
}
