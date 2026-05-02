using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    public enum TileType
    {
        blank, cross, naught
    }
    public enum GameState
    {
        playing, draw, crossWins, naughtWins
    }

    public class TicTacToe
    {
        //TODO change myTurn to false and playerTileType to blank for defaults
        //they should be dictated by the server
        public bool myTurn = true;
        public TileType playerTileType = TileType.cross;
        public List<Button> buttons = new List<Button>();//assuming 9 in order
        public TileType[] grid = new TileType[9];

        // Stores whether this client is Player1 or Player2
        public string playerName = "";

        public string GridToString()
        {
            string s = "";
            // Convert values on board to a string e.g "xox___x_o"
            for (int i = 0; i < buttons.Count; i++)
            {
                switch (buttons[i].Text)
                    {
                    case "X":
                        s += 'x';
                        break;
                    case "O":
                        s += 'o';
                        break;
                    default:
                        s += '-';
                        break;
                }
            }    
            return s;
        }
        public void StringToGrid(string s)
        {
            //TODO take string s e.g "xox___x_o" and use its values to update grid and the buttons
            for (int i = 0; i < grid.Length; i++)
            {
                switch (s[i])
                {
                    case 'x':
                        grid[i] = TileType.cross;
                        break;
                    case 'o':
                        grid[i] = TileType.naught;
                        break;
                    default:
                        grid[i] = TileType.blank;
                        break;
                }
                if (buttons.Count >= 9) 
                {
                    buttons[i].Invoke((Action)delegate
                    {
                        buttons[i].Text = TileTypeToString(grid[i]);
                        ApplyTileStyle(buttons[i], grid[i]);

                        //buttons[i].Text = TileTypeToString(grid[i]);
                    });
                }
            }
        }

        public bool SetTile(int index, TileType tileType)
        {
            if (index < 0 || index > 8)
                return false;

            if (grid[index] == TileType.blank)
            {
                grid[index] = tileType;
                if (buttons.Count >= 9)
                {
                    buttons[index].Invoke((Action)delegate
                    {
                        buttons[index].Text = TileTypeToString(tileType);
                        ApplyTileStyle(buttons[index], tileType);
                    });
                }
                return true;
            }
            return false;
        }

        private void ApplyTileStyle(Button btn, TileType tile)
        {
            if (tile == TileType.cross)
            {
                btn.BackColor = Color.FromArgb(186, 147, 255); // lavender
                btn.ForeColor = Color.White;
            }
            else if (tile == TileType.naught)
            {
                btn.BackColor = Color.FromArgb(221, 160, 221); // lilac
                btn.ForeColor = Color.White;
            }
            else
            {
                // Empty tile: restore original button look
                btn.BackColor = SystemColors.Control;
                btn.ForeColor = Color.Black;
            }
        }

        public GameState GetGameState()
        {
            GameState state = GameState.playing;
            if (CheckForWin(TileType.cross))
                state = GameState.crossWins;
            else if (CheckForWin(TileType.naught))
                state = GameState.naughtWins;
            else if (CheckForDraw())
                state = GameState.draw;


            return state;
        }
        public bool CheckForWin(TileType t)
        {
            //horizontals
            if (grid[0] == t && grid[1] == t && grid[2] == t)
                return true;
            if (grid[3] == t && grid[4] == t && grid[5] == t)
                return true;
            if (grid[6] == t && grid[7] == t && grid[8] == t)
                return true;

            //verticals
            if (grid[0] == t && grid[3] == t && grid[6] == t)
                return true;
            if (grid[1] == t && grid[4] == t && grid[7] == t)
                return true;
            if (grid[2] == t && grid[5] == t && grid[8] == t)
                return true;

            //diagonals
            if (grid[0] == t && grid[4] == t && grid[8] == t)
                return true;
            if (grid[2] == t && grid[4] == t && grid[6] == t)
                return true;


            //nothing
            return false;
        }

        public bool CheckForDraw()
        {
            for(int i = 0; i < 9; i++)
            {
                if (grid[i] == TileType.blank)
                    return false;
            }

            return true;
        }

        public void ResetBoard()
        {
            for (int i = 0; i < 9; i++)
            {
                grid[i] = TileType.blank;

                if (buttons.Count >= 9)
                {
                    int index = i; // important for Invoke closure

                    buttons[index].Invoke((Action)delegate
                    {
                        buttons[index].Text = "";

                        // Restore original look
                        buttons[index].BackColor = SystemColors.Control;
                        buttons[index].ForeColor = Color.Black;
                    });
                }
            }
        }

        public static string TileTypeToString(TileType t)
        {
            if (t == TileType.blank)
                return "";
            else if (t == TileType.cross)
                return "X";
            else
                return "O";
        }

        public void HighlightAvailableTiles()
        {
            for (int i = 0; i < 9; i++)
            {
                if (buttons.Count >= 9 && grid[i] == TileType.blank)
                {
                    int index = i;

                    buttons[index].Invoke((Action)delegate
                    {
                        buttons[index].BackColor = Color.Lavender;
                    });
                }
            }
        }
    }
}
