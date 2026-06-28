using System;
using System.Collections.Generic;
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

        // FIKSIRANA PROVERA ŠAHA: Eksponiramo fabričku metodu iz biblioteke
        public bool IsInCheck(Player player) => _game.IsInCheck(player);

        // TAČKICE: Izvlačimo sve validne poteze za figuru na zadatoj poziciji
        public List<Position> GetValidMovesForPiece(int row, int col)
        {
            List<Position> validDestinations = new List<Position>();
            Position currentPos = ToPosition(row, col);

            // Uzimamo apsolutno sve legalne poteze za trenutnog igrača na potezu
            var allMoves = _game.GetValidMoves(_game.WhoseTurn);

            foreach (var move in allMoves)
            {
                // Ako potez kreće sa našeg selektovanog polja, pamtimo gde ide
                if (move.OriginalPosition.Equals(currentPos))
                {
                    validDestinations.Add(move.NewPosition);
                }
            }
            return validDestinations;
        }

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

            if (piece.GetFenCharacter() == 'P' || piece.GetFenCharacter() == 'p')
            {
                if (toPos.Rank == 8 || toPos.Rank == 1)
                {
                    if (promotionChar == null)
                    {
                        reason = "PROMOCIJA";
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
        public string GetFen()
        {
            System.Text.StringBuilder fen = new System.Text.StringBuilder();

            // 1. Prolazimo kroz tablu red po red (od 8. do 1. reda)
            for (int r = 0; r < 8; r++)
            {
                int emptyCount = 0;
                for (int c = 0; c < 8; c++)
                {
                    string piece = GetPieceAt(r, c);
                    if (piece == ".")
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            fen.Append(emptyCount);
                            emptyCount = 0;
                        }
                        fen.Append(piece);
                    }
                }
                if (emptyCount > 0)
                {
                    fen.Append(emptyCount);
                }
                if (r < 7)
                {
                    fen.Append("/");
                }
            }

            // 2. Ko je na potezu (w = beli, b = crni)
            string turn = CurrentTurn == ChessDotNet.Player.White ? "w" : "b";
            fen.Append($" {turn}");

            // 3. Prava na rokadu i en passant (stavljamo bazične vrednosti za stabilan proračun bota)
            fen.Append(" KQkq - 0 1");

            return fen.ToString();
        }

    }
}
