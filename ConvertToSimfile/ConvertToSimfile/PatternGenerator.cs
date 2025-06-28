namespace ConvertToSimfile
{
    internal class PatternGenerator
    {
        private readonly Random _random;
        private readonly GenerationOptions _generationOptions;

        private int _lastArrow = -1;
        private int _secondLastArrow = -1;
        private int _stepsSinceJump = 0;

        public PatternGenerator(
            int seed,
            GenerationOptions generationOptions)
        {
            _random = new Random(seed);
            _generationOptions = generationOptions;
        }

        public string GetNextPattern(int stepIndex)
        {
            if (_generationOptions == GenerationOptions.RandomSingleArrows)
            {
                return SimfileConstants.SINGLE_ARROWS[_random.Next(4)];
            }

            if (_generationOptions == GenerationOptions.PatternsNoJumps)
            {
                return GenerateSingleArrow(stepIndex);
            }

            // Increase jump probability as song progresses
            double jumpChance = Math.Min(0.3, stepIndex * 0.001);

            // Reduce jump frequency if we just had one
            if (_stepsSinceJump < 3)
            {
                jumpChance *= 0.3;
            }

            // Decide whether to place a jump
            if (_random.NextDouble() < jumpChance)
            {
                _stepsSinceJump = 0;
                return GenerateJump();
            }
            else
            {
                ++_stepsSinceJump;
                return GenerateSingleArrow(stepIndex);
            }
        }

        private string GenerateJump()
        {
            // Choose a jump that doesn't conflict with recent arrows
            var validJumps = SimfileConstants.JUMP_PATTERNS.ToList();

            // Remove jumps that would repeat recent arrows too much
            if (_lastArrow >= 0)
            {
                validJumps = [.. validJumps.Where(jump => !IsJumpTooSimilar(jump, _lastArrow))];
            }

            if (validJumps.Count == 0)
            {
                validJumps = [.. SimfileConstants.JUMP_PATTERNS];
            }

            var selectedJump = validJumps[_random.Next(validJumps.Count)];

            // Update tracking
            _lastArrow = -1; // Reset single arrow tracking after jump
            _secondLastArrow = -1;

            return selectedJump;
        }

        private bool IsJumpTooSimilar(string jump, int lastArrow)
        {
            // Check if the jump contains the last arrow
            return jump[lastArrow] == '1';
        }

        private string GenerateSingleArrow(int stepIndex)
        {
            var validArrows = new List<int> { 0, 1, 2, 3 }; // L, D, U, R

            // Remove arrows that would create awkward patterns
            if (_lastArrow >= 0)
            {
                // Don't repeat the same arrow immediately
                validArrows.Remove(_lastArrow);

                // Avoid three-in-a-row of the same arrow
                if (_secondLastArrow == _lastArrow)
                {
                    validArrows.Remove(_lastArrow);
                }
            }

            // Create flowing patterns (crossovers and streams)
            if (_lastArrow >= 0 && validArrows.Count > 1)
            {
                // Favor stream patterns (L->D->U->R or reverse)
                var streamArrows = GetStreamArrows(_lastArrow);
                var availableStreamArrows = streamArrows.Where(a => validArrows.Contains(a)).ToList();

                if (availableStreamArrows.Count > 0 && _random.NextDouble() < 0.6)
                {
                    validArrows = availableStreamArrows;
                }
                // Favor crossover patterns occasionally
                else if (_random.NextDouble() < 0.2)
                {
                    var crossoverArrows = GetCrossoverArrows(_lastArrow);
                    var availableCrossovers = crossoverArrows.Where(a => validArrows.Contains(a)).ToList();
                    if (availableCrossovers.Count > 0)
                    {
                        validArrows = availableCrossovers;
                    }
                }
            }

            // Select the arrow
            int selectedArrow = validArrows[_random.Next(validArrows.Count)];

            // Update tracking
            _secondLastArrow = _lastArrow;
            _lastArrow = selectedArrow;

            return SimfileConstants.SINGLE_ARROWS[selectedArrow];
        }

        private static List<int> GetStreamArrows(int lastArrow)
        {
            // Create flowing stream patterns
            var streams = new List<int>();

            switch (lastArrow)
            {
                case 0: // Left -> Down or Up
                    streams.AddRange(new[] { 1, 2 });
                    break;
                case 1: // Down -> Left, Up, or Right
                    streams.AddRange(new[] { 0, 2, 3 });
                    break;
                case 2: // Up -> Left, Down, or Right  
                    streams.AddRange(new[] { 0, 1, 3 });
                    break;
                case 3: // Right -> Down or Up
                    streams.AddRange(new[] { 1, 2 });
                    break;
            }

            return streams;
        }

        private static List<int> GetCrossoverArrows(int lastArrow)
        {
            // Create crossover patterns (opposite arrows)
            switch (lastArrow)
            {
                case 0: return new List<int> { 3 }; // Left -> Right
                case 1: return new List<int> { 2 }; // Down -> Up  
                case 2: return new List<int> { 1 }; // Up -> Down
                case 3: return new List<int> { 0 }; // Right -> Left
                default: return new List<int>();
            }
        }
    }
}
