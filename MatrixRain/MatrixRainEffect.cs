using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MatrixRainEffectDemo
{
    /// <summary>
    /// Encapsulates the Matrix Rain effect with its own text window.
    /// </summary>
    public class MatrixRainEffect
    {
        private readonly char[] _chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@#$%^&*()".ToCharArray();
        private const int TrailLength = 2, ClearOffset = 20, Delay = 20;
        private readonly object _consoleLock = new();

        // Configurable window size for inner content area.
        private int _maxContentWidth;
        private int _maxContentHeight;

        // Calculated frame boundaries.
        private int _windowLeft, _windowRight, _windowTop, _windowBottom;
        // Indicates whether the text window (frame) is visible.
        private volatile bool _windowVisible = false;
        // Flag to track if the frame area has been cleared already.
        private bool _frameCleared = false;

        private readonly List<string> _textBuffer = new();

        // We capture the original Console.Out so our internal writes bypass the custom writer.
        private readonly TextWriter _originalOut;
        private Thread _rainThread;
        private Thread _textWindowThread;
        private volatile bool _stopRequested = false;
        private volatile bool _textWindowStopRequested = false;
        private readonly Random _rand = new();

        // Cached row updates to reduce per-frame allocations.
        private SortedList<int, (char ch, ConsoleColor color)>[] _cachedRowUpdates;
        // Cached last drawn text window lines.
        private string[] _lastTextWindowLines;

        /// <summary>
        /// Creates a MatrixRainEffect with an (initially hidden) text window.
        /// The text window will have an inner content area no wider than maxContentWidth and no taller than maxContentHeight.
        /// The overall drawn frame adds a 1-character border on each side.
        /// </summary>
        public MatrixRainEffect(TextWriter originalOut, int maxContentWidth = 80, int maxContentHeight = 15)
        {
            _originalOut = originalOut;
            _maxContentWidth = maxContentWidth;
            _maxContentHeight = maxContentHeight;
            Console.CursorVisible = false;
            UpdateWindowDimensions();
        }

        /// <summary>
        /// Starts the matrix rain animation and text window refresh on background threads.
        /// </summary>
        public void Start()
        {
            _rainThread = new Thread(MatrixAnimation) { IsBackground = true };
            _rainThread.Start();

            _textWindowThread = new Thread(TextWindowAnimation) { IsBackground = true };
            _textWindowThread.Start();
        }

        /// <summary>
        /// Stops both the matrix animation and text window refresh gracefully.
        /// </summary>
        public void Stop()
        {
            _stopRequested = true;
            _textWindowStopRequested = true;
            _rainThread?.Join();
            _textWindowThread?.Join();
        }

        /// <summary>
        /// Dynamically updates the window size.
        /// </summary>
        public void SetWindowSize(int maxContentWidth, int maxContentHeight)
        {
            lock (_consoleLock)
            {
                _maxContentWidth = maxContentWidth;
                _maxContentHeight = maxContentHeight;
                UpdateWindowDimensions();
                if (_windowVisible)
                {
                    DrawTextWindowFrame();
                    RedrawTextWindowContent();
                }
                else
                {
                    ClearWindowFrameArea();
                    _frameCleared = true;
                }
            }
        }

        /// <summary>
        /// Clears the text buffer and hides the text window frame.
        /// </summary>
        public void ClearText()
        {
            lock (_textBuffer)
            {
                _textBuffer.Clear();
            }
            _windowVisible = false;
            lock (_consoleLock)
            {
                ClearWindowFrameArea();
                _frameCleared = true;
            }
        }

        /// <summary>
        /// Clears the area where the text window frame would be.
        /// </summary>
        private void ClearWindowFrameArea()
        {
            try
            {
                int width = _windowRight - _windowLeft + 1;
                for (int y = _windowTop; y <= _windowBottom; y++)
                {
                    Console.SetCursorPosition(_windowLeft, y);
                    _originalOut.Write(new string(' ', width));
                }
            }
            catch { /* Ignored */ }
        }

        /// <summary>
        /// Updates the text window’s position and dimensions so that it is centered.
        /// </summary>
        private void UpdateWindowDimensions()
        {
            int consoleWidth = Console.WindowWidth;
            int consoleHeight = Console.WindowHeight;
            int contentWidth = Math.Min(_maxContentWidth, consoleWidth - 2);
            int contentHeight = Math.Min(_maxContentHeight, consoleHeight - 2);
            _windowLeft = (consoleWidth - (contentWidth + 2)) / 2;
            _windowTop = (consoleHeight - (contentHeight + 2)) / 2;
            _windowRight = _windowLeft + contentWidth + 1;
            _windowBottom = _windowTop + contentHeight + 1;
        }

        /// <summary>
        /// Draws the frame for the text window.
        /// </summary>
        private void DrawTextWindowFrame()
        {
            lock (_consoleLock)
            {
                try
                {
                    int contentWidth = _windowRight - _windowLeft - 1;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.SetCursorPosition(_windowLeft, _windowTop);
                    _originalOut.Write($"┌{new string('─', contentWidth)}┐");
                    for (int y = _windowTop + 1; y < _windowBottom; y++)
                    {
                        Console.SetCursorPosition(_windowLeft, y);
                        _originalOut.Write("│");
                        Console.SetCursorPosition(_windowRight, y);
                        _originalOut.Write("│");
                    }
                    Console.SetCursorPosition(_windowLeft, _windowBottom);
                    _originalOut.Write($"└{new string('─', contentWidth)}┘");
                }
                catch { /* Ignored */ }
            }
        }

        /// <summary>
        /// Adds a line of text to the text window.
        /// </summary>
        public void PrintLine(string text)
        {
            lock (_textBuffer)
            {
                _textBuffer.Add(text);
                if (_textBuffer.Count > 1000)
                    _textBuffer.RemoveRange(0, _textBuffer.Count - 1000);
            }
            if (!_windowVisible)
            {
                _windowVisible = true;
                _frameCleared = false;
                lock (_consoleLock)
                {
                    DrawTextWindowFrame();
                }
            }
        }

        /// <summary>
        /// Periodically refreshes the text window area.
        /// </summary>
        private void TextWindowAnimation()
        {
            while (!_textWindowStopRequested)
            {
                RedrawTextWindowContent();
                Thread.Sleep(200);
            }
        }

        private void RedrawTextWindowContent()
        {
            int contentWidth, contentHeight, windowLeft, windowTop;
            lock (_consoleLock)
            {
                contentWidth = _windowRight - _windowLeft - 1;
                contentHeight = _windowBottom - _windowTop - 1;
                windowLeft = _windowLeft;
                windowTop = _windowTop;
            }
            string[] newLines;
            lock (_textBuffer)
            {
                int startLine = Math.Max(0, _textBuffer.Count - contentHeight);
                newLines = _textBuffer.Skip(startLine).Take(contentHeight).ToArray();
            }
            if (newLines.Length == 0)
            {
                lock (_consoleLock)
                {
                    if (!_frameCleared)
                    {
                        ClearWindowFrameArea();
                        _frameCleared = true;
                    }
                }
                return;
            }
            if (_lastTextWindowLines == null || _lastTextWindowLines.Length != contentHeight)
                _lastTextWindowLines = new string[contentHeight];
            lock (_consoleLock)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    for (int i = 0; i < contentHeight; i++)
                    {
                        string newLine = i < newLines.Length ? newLines[i] : "";
                        if (newLine.Length > contentWidth)
                            newLine = newLine.Substring(0, contentWidth);
                        else
                            newLine = newLine.PadRight(contentWidth);
                        if (_lastTextWindowLines[i] != newLine)
                        {
                            Console.SetCursorPosition(windowLeft + 1, windowTop + 1 + i);
                            _originalOut.Write(newLine);
                            _lastTextWindowLines[i] = newLine;
                        }
                    }
                }
                catch { /* Ignored */ }
            }
        }

        /// <summary>
        /// Runs the matrix rain animation.
        /// </summary>
        private void MatrixAnimation()
        {
            int width, height;
            lock (_consoleLock)
            {
                width = Console.WindowWidth;
                height = Console.WindowHeight;
            }
            int[] drops = new int[width];
            for (int i = 0; i < width; i++)
                drops[i] = _rand.Next(height);
            int charsLength = _chars.Length;
            // Preallocate a StringBuilder for grouping updates.
            StringBuilder sb = new StringBuilder(128);

            while (!_stopRequested)
            {
                int currentWidth = Console.WindowWidth;
                int currentHeight = Console.WindowHeight;
                if (currentWidth != width || currentHeight != height)
                {
                    lock (_consoleLock)
                    {
                        try
                        {
                            width = Console.WindowWidth;
                            height = Console.WindowHeight;
                            Console.Clear();
                            UpdateWindowDimensions();
                            if (_windowVisible)
                                DrawTextWindowFrame();
                            else
                            {
                                ClearWindowFrameArea();
                                _frameCleared = true;
                            }
                            drops = new int[width];
                            for (int i = 0; i < width; i++)
                                drops[i] = _rand.Next(height);
                            _cachedRowUpdates = null;
                        }
                        catch { }
                    }
                }

                int localWidth = width, localHeight = height;
                int localWindowLeft, localWindowRight, localWindowTop, localWindowBottom;
                lock (_consoleLock)
                {
                    localWindowLeft = _windowLeft;
                    localWindowRight = _windowRight;
                    localWindowTop = _windowTop;
                    localWindowBottom = _windowBottom;
                }

                // Reuse or allocate row updates.
                SortedList<int, (char ch, ConsoleColor color)>[] rowUpdates;
                if (_cachedRowUpdates == null || _cachedRowUpdates.Length != localHeight)
                {
                    rowUpdates = new SortedList<int, (char, ConsoleColor)>[localHeight];
                    _cachedRowUpdates = rowUpdates;
                }
                else
                {
                    rowUpdates = _cachedRowUpdates;
                    for (int i = 0; i < rowUpdates.Length; i++)
                        rowUpdates[i]?.Clear();
                }

                // Local helper to add an update.
                void AddUpdate(int x, int y, char ch, ConsoleColor color)
                {
                    if (_windowVisible && x >= localWindowLeft && x <= localWindowRight && y >= localWindowTop && y <= localWindowBottom)
                        return;
                    if (y < 0 || y >= localHeight)
                        return;
                    if (rowUpdates[y] == null)
                        rowUpdates[y] = new SortedList<int, (char, ConsoleColor)>();
                    rowUpdates[y][x] = (ch, color);
                }

                for (int x = 0; x < localWidth; x++)
                {
                    int dropY = drops[x];
                    AddUpdate(x, dropY, _chars[_rand.Next(charsLength)], ConsoleColor.White);
                    for (int j = 1; j <= TrailLength; j++)
                    {
                        int trailY = (dropY - j + localHeight) % localHeight;
                        AddUpdate(x, trailY, _chars[_rand.Next(charsLength)], ConsoleColor.DarkGreen);
                    }
                    int clearY = (dropY - ClearOffset + localHeight) % localHeight;
                    AddUpdate(x, clearY, ' ', Console.ForegroundColor);
                    drops[x] = (dropY + 1) % localHeight;
                }

                // Flush all updates.
                lock (_consoleLock)
                {
                    try
                    {
                        for (int row = 0; row < localHeight; row++)
                        {
                            var updates = rowUpdates[row];
                            if (updates == null || updates.Count == 0)
                                continue;

                            sb.Clear();
                            int? groupStart = null;
                            ConsoleColor? groupColor = null;
                            int previousX = -2;
                            foreach (var kvp in updates)
                            {
                                int col = kvp.Key;
                                var (ch, color) = kvp.Value;
                                if (groupStart == null)
                                {
                                    groupStart = col;
                                    groupColor = color;
                                    sb.Append(ch);
                                    previousX = col;
                                }
                                else if (col == previousX + 1 && color == groupColor)
                                {
                                    sb.Append(ch);
                                    previousX = col;
                                }
                                else
                                {
                                    Console.SetCursorPosition(groupStart.Value, row);
                                    Console.ForegroundColor = groupColor.Value;
                                    _originalOut.Write(sb.ToString());
                                    groupStart = col;
                                    groupColor = color;
                                    sb.Clear();
                                    sb.Append(ch);
                                    previousX = col;
                                }
                            }
                            if (groupStart.HasValue)
                            {
                                Console.SetCursorPosition(groupStart.Value, row);
                                Console.ForegroundColor = groupColor.Value;
                                _originalOut.Write(sb.ToString());
                            }
                        }
                    }
                    catch { }
                }

                Thread.Sleep(Delay);
            }
        }
    }

    /// <summary>
    /// A custom TextWriter that redirects output to a MatrixRainEffect text window.
    /// </summary>
    public class MatrixRainWriter : TextWriter
    {
        private readonly MatrixRainEffect _effect;
        public MatrixRainWriter(MatrixRainEffect effect) => _effect = effect;
        public override Encoding Encoding => Encoding.Default;
        public override void WriteLine(string value) => _effect.PrintLine(value);
        public override void Write(char value) => _effect.PrintLine(value.ToString());
    }
}
