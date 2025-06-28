using System.Text;

namespace ConvertToSimfile
{
    internal class ZorbaConverter
    {
        private const double _baseBpm = 60.0; // Adjust this to change arrow speed
        private const double _secsPerMin = 60.0;
        private const int _seed = 1964; // Fixed seed for consistent results

        private record StepEvent(double Time, double Beat, string Pattern);

        private record BpmChange(double Bpm, double Beat);

        public string ConvertToSimfile()
        {
            var easySteps = GenerateSteps(GenerationOptions.RandomSingleArrows);
            var mediumSteps = GenerateSteps(GenerationOptions.PatternsNoJumps);
            var hardSteps = GenerateSteps(GenerationOptions.PatternsWithJumps);

            var bpmChanges = GenerateBpmChanges();

            return BuildSimfile(easySteps, mediumSteps, hardSteps, bpmChanges);
        }

        private static List<StepEvent> GenerateSteps(GenerationOptions generationOptions)
        {
            var steps = new List<StepEvent>();

            var patternGenerator = new PatternGenerator(_seed, generationOptions);

            for (var i = 0; i < FlashTimings.STEP_TIMES.Length; ++i)
            {
                var adjustedTime = GetAdjustedTimeByIndex(i);
                var beat = GetBeatFromTime(adjustedTime);
                var pattern = patternGenerator.GetNextPattern(i);

                steps.Add(new StepEvent(adjustedTime, beat, pattern));
            }

            return [.. steps.OrderBy(s => s.Time)];
        }

        private static List<BpmChange> GenerateBpmChanges()
        {
            var bpmChanges = new List<BpmChange>();

            // Start with base BPM
            bpmChanges.Add(new BpmChange(_baseBpm, 0));

            // Add BPM changes based on speed increases
            int stepIndex = 0;
            for (int turnIndex = 0; turnIndex < FlashTimings.TURN_LENGTHS.Length && turnIndex < FlashTimings.SPEEDS.Length; turnIndex++)
            {
                var turnLength = FlashTimings.TURN_LENGTHS[turnIndex];
                if (turnLength == -1)
                {
                    break; // End of turns
                }

                var speed = FlashTimings.SPEEDS[turnIndex];
                var newBpm = _baseBpm * (speed / 1.5); // Scale relative to initial speed

                if (stepIndex < FlashTimings.STEP_TIMES.Length)
                {
                    var time = GetAdjustedTimeByIndex(stepIndex);
                    var beat = GetBeatFromTime(time);

                    bpmChanges.Add(new BpmChange(newBpm, beat));
                }

                stepIndex += Math.Min(turnLength, FlashTimings.STEP_TIMES.Length - stepIndex);
            }

            return [.. bpmChanges.OrderBy(b => b.Beat)];
        }

        private static double GetAdjustedTimeByIndex(int index)
        {
            return FlashTimings.STEP_TIMES[index] + FlashTimings.STEP_DELAY + FlashTimings.DELAY + FlashTimings.OFFSET;
        }

        private static double GetBeatFromTime(double time)
        {
            return (time / _secsPerMin) * _baseBpm;
        }

        private string BuildSimfile(
            List<StepEvent> easySteps,
            List<StepEvent> mediumSteps,
            List<StepEvent> hardSteps,
            List<BpmChange> bpmChanges)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("#TITLE:Zorba;");
            sb.AppendLine("#SUBTITLE:;");
            sb.AppendLine("#ARTIST:Mikis Theodorakis;");
            sb.AppendLine("#TITLETRANSLIT:;");
            sb.AppendLine("#SUBTITLETRANSLIT:;");
            sb.AppendLine("#ARTISTTRANSLIT:;");
            sb.AppendLine("#GENRE:Folk;");
            sb.AppendLine("#CREDIT:Pippin Barr;");
            sb.AppendLine("#BANNER:;");
            sb.AppendLine("#BACKGROUND:;");
            sb.AppendLine("#LYRICSPATH:;");
            sb.AppendLine("#CDTITLE:;");
            sb.AppendLine("#MUSIC:Zorba.mp3;");
            sb.AppendLine($"#OFFSET:{FlashTimings.STEP_DELAY + FlashTimings.DELAY:F3};");
            sb.AppendLine("#SAMPLESTART:70.500;");
            sb.AppendLine("#SAMPLELENGTH:20.000;");
            sb.AppendLine("#SELECTABLE:YES;");

            // BPM changes
            sb.Append("#BPMS:");
            for (int i = 0; i < bpmChanges.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append($"{bpmChanges[i].Beat:F3}={bpmChanges[i].Bpm:F3}");
            }
            sb.AppendLine(";");

            sb.AppendLine("#STOPS:;");
            sb.AppendLine("#FREEZES:;");
            sb.AppendLine("");

            // Chart data
            sb.AppendLine("#NOTES:");
            sb.AppendLine("     dance-single:");
            sb.AppendLine("     :");
            sb.AppendLine("     Easy:");
            sb.AppendLine("     12:");
            sb.AppendLine("     0,0,0,0,0:");
            sb.Append(ConvertToBeatNotation(easySteps, bpmChanges));
            sb.AppendLine(";");

            sb.AppendLine("#NOTES:");
            sb.AppendLine("     dance-single:");
            sb.AppendLine("     :");
            sb.AppendLine("     Medium:");
            sb.AppendLine("     12:");
            sb.AppendLine("     0,0,0,0,0:");
            sb.Append(ConvertToBeatNotation(mediumSteps, bpmChanges));
            sb.AppendLine(";");

            sb.AppendLine("#NOTES:");
            sb.AppendLine("     dance-single:");
            sb.AppendLine("     :");
            sb.AppendLine("     Hard:");
            sb.AppendLine("     12:");
            sb.AppendLine("     0,0,0,0,0:");
            sb.Append(ConvertToBeatNotation(hardSteps, bpmChanges));
            sb.AppendLine(";");

            return sb.ToString();
        }

        private List<StepEvent> RecalculateBeatsWithBpmChanges(List<StepEvent> steps, List<BpmChange> bpmChanges)
        {
            var result = new List<StepEvent>();

            foreach (var step in steps)
            {
                double beat = CalculateBeatAtTime(step.Time, bpmChanges);
                result.Add(new StepEvent(step.Time, beat, step.Pattern));
            }

            return result;
        }

        private double CalculateBeatAtTime(double time, List<BpmChange> bpmChanges)
        {
            double currentBeat = 0;
            double currentTime = 0;
            double currentBpm = _baseBpm;

            // Find the appropriate BPM for this time
            foreach (var bpmChange in bpmChanges.OrderBy(b => b.Beat))
            {
                // Convert beat back to time for this BPM change
                double bpmChangeTime = currentTime + ((bpmChange.Beat - currentBeat) / currentBpm) * 60.0;

                if (bpmChangeTime <= time)
                {
                    // Update our position
                    currentBeat = bpmChange.Beat;
                    currentTime = bpmChangeTime;
                    currentBpm = bpmChange.Bpm;
                }
                else
                {
                    break;
                }
            }

            // Calculate the final beat position
            double remainingTime = time - currentTime;
            double additionalBeats = (remainingTime / 60.0) * currentBpm;

            return currentBeat + additionalBeats;
        }

        private string ConvertToBeatNotation(List<StepEvent> steps, List<BpmChange> bpmChanges)
        {
            var sb = new StringBuilder();
            const double RESOLUTION = 192; // 192nd notes (common Stepmania resolution)

            if (steps.Count == 0) return "";

            // Recalculate beats using proper BPM changes
            var recalculatedSteps = RecalculateBeatsWithBpmChanges(steps, bpmChanges);

            double maxBeat = recalculatedSteps.Max(s => s.Beat) + 4; // Add some padding
            int totalBeats = (int)Math.Ceiling(maxBeat / 4) * 4; // Round up to nearest measure

            // Create a dictionary for quick step lookup
            var stepsByBeat = new Dictionary<int, StepEvent>();
            foreach (var step in recalculatedSteps)
            {
                int quantizedBeat = (int)Math.Round(step.Beat * RESOLUTION / 4);
                stepsByBeat[quantizedBeat] = step;
            }

            // Generate the step chart
            for (int measure = 0; measure < totalBeats / 4; measure++)
            {
                var measureLines = new List<string>();

                // Process each 192nd note in this measure
                for (int tick = 0; tick < RESOLUTION; tick++)
                {
                    int absoluteTick = measure * (int)RESOLUTION + tick;

                    if (stepsByBeat.ContainsKey(absoluteTick))
                    {
                        measureLines.Add(stepsByBeat[absoluteTick].Pattern);
                    }
                    else
                    {
                        measureLines.Add("0000");
                    }
                }

                // Only include lines that are on common subdivisions or have steps
                var filteredLines = new List<string>();
                for (int i = 0; i < measureLines.Count; i++)
                {
                    // Include if it's a step, or on a common subdivision
                    if (measureLines[i] != "0000" ||
                        i % 48 == 0 ||  // 4th notes
                        i % 24 == 0 ||  // 8th notes
                        i % 12 == 0 ||  // 16th notes
                        i % 6 == 0)     // 32nd notes
                    {
                        filteredLines.Add(measureLines[i]);
                    }
                }

                // If no steps in this measure, just add a minimal representation
                if (filteredLines.All(line => line == "0000"))
                {
                    filteredLines.Clear();
                    filteredLines.Add("0000");
                    filteredLines.Add("0000");
                    filteredLines.Add("0000");
                    filteredLines.Add("0000");
                }

                // Add measure to output
                foreach (string line in filteredLines)
                {
                    sb.AppendLine(line);
                }

                // Add measure separator (except for last measure)
                if (measure < (totalBeats / 4) - 1)
                {
                    sb.AppendLine(",");
                }
            }

            return sb.ToString();
        }
    }
}
