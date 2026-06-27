using Godot;
using System;

namespace ChessPuzzles2d.Services
{
    public class PieceAtlasService
    {
        private readonly Texture2D _atlasTexture;
        private readonly float _pieceWidth;
        private readonly float _pieceHeight;

        public PieceAtlasService(Texture2D atlasTexture)
        {
            _atlasTexture = atlasTexture ?? throw new ArgumentNullException(nameof(atlasTexture));

            // Wikimedia slika ima 6 kolona i 2 reda
            _pieceWidth = _atlasTexture.GetSize().X / 6f;
            _pieceHeight = _atlasTexture.GetSize().Y / 2f;
        }

        public bool IsAtlasLoaded => _atlasTexture != null;

        public AtlasTexture GetPieceTexture(string pieceCode)
        {
            if (string.IsNullOrEmpty(pieceCode) || pieceCode == ".") return null;

            AtlasTexture atlasPiece = new AtlasTexture { Atlas = _atlasTexture };
            int colIndex = 0;

            // Veliko slovo = gornji red na slici (Beli), malo slovo = donji red (Crni)
            int rowIndex = char.IsUpper(pieceCode[0]) ? 1 : 0;

            // VAŽNO: Usklađeno sa Wikimedia rasporedom slika
            switch (pieceCode.ToLower())
            {
                case "q": colIndex = 0; break; // Kraljica je prva
                case "k": colIndex = 1; break; // Kralj je drugi
                case "r": colIndex = 2; break; // Top
                case "n": colIndex = 3; break; // Skakač
                case "b": colIndex = 4; break; // Lovac
                case "p": colIndex = 5; break; // Pešak
            }

            atlasPiece.Region = new Rect2(
                colIndex * _pieceWidth,
                rowIndex * _pieceHeight,
                _pieceWidth,
                _pieceHeight
            );

            return atlasPiece;
        }
    }
}
