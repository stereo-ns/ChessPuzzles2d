using System;
using ChessDotNet;

namespace ChessPuzzles2d.Core
{
    public class ChessBoardState
    {
        private readonly ChessGame _game;

        public ChessBoardState()
        {
            _game = new ChessGame();
        }

        public Player CurrentTurn => _game.WhoseTurn;

        private Position ToPosition(int row, int col)
        {
            File file = (File)col;
            int rank = 8 - row;
            return new Position(file, rank);
        }

        public string GetPieceAt(int row, int col)
        {
            Position pos = ToPosition(row, col);
            Piece piece = _game.GetPieceAt(pos);
            if (piece == null) return ".";

            char fenChar = piece.GetFenCharacter();
            return fenChar.ToString();
        }

        public bool IsCheckmated(Player player) => _game.IsCheckmated(player);
        public bool IsDraw() => _game.IsDraw();

        public bool TryMakeMove(int fromRow, int fromCol, int toRow, int toCol, char? promotionChar, out string reason)
        {
            reason = string.Empty;

            Position fromPos = ToPosition(fromRow, fromCol);
            Position toPos = ToPosition(toRow, toCol);

            Piece piece = _game.GetPieceAt(fromPos);
            if (piece == null)
            {
                reason = "Izabrano polje je prazno.";
                return false;
            }

            if (piece.Owner != _game.WhoseTurn)
            {
                reason = "Nije vaš red!";
                return false;
            }

            // PROVERA PROMOCIJE: Da li pešak stupa na poslednji red (Rank 8 ili Rank 1)
            if (piece.GetFenCharacter() == 'P' || piece.GetFenCharacter() == 'p')
            {
                if (toPos.Rank == 8 || toPos.Rank == 1)
                {
                    if (promotionChar == null)
                    {
                        reason = "PROMOCIJA"; // Signaliziramo da nam treba vizuelni meni
                        return false;
                    }
                }
            }

            Move move = new Move(fromPos, toPos, _game.WhoseTurn, promotionChar);

            if (_game.IsValidMove(move))
            {
                _game.MakeMove(move, true);
                return true;
            }

            reason = "Potez je nelegalan.";
            return false;
        }
    }
}
