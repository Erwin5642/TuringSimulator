using System;
using System.Collections.Generic;
using TuringSimulator.Core.ProgramGraph;
using TuringSimulator.Core.Types;

namespace ITS
{
    /// <summary>Serializes an authored block graph into factory-language ITS JSON.</summary>
    public static class ItsBenchProgramSerializer
    {
        public const int MaxBlocks = 32;

        public const string TipoMovimento = "movimento";
        public const string TipoMateriais = "materiais";
        public const string TipoCondicao = "condicao";
        public const string TipoAceitar = "aceitar";
        public const string TipoRejeitar = "rejeitar";

        public const string PortaSaida = "saida";
        public const string PortaVerdadeiro = "verdadeiro";
        public const string PortaFalso = "falso";

        public const string CartaoEsquerda = "esquerda";
        public const string CartaoDireita = "direita";

        public static AskProgramDto FromSnapshot(ProgramGraphSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var nodes = snapshot.Nodes ?? Array.Empty<ProgramGraphNodeData>();
            var limit = Math.Min(nodes.Count, MaxBlocks);
            var keptIds = new HashSet<string>(StringComparer.Ordinal);
            var blocos = new List<AskProgramBlockDto>(limit);
            for (var i = 0; i < limit; i++)
            {
                var node = nodes[i];
                if (node.BlockId == null)
                    continue;
                keptIds.Add(node.BlockId);
                blocos.Add(new AskProgramBlockDto
                {
                    Id = node.BlockId,
                    Tipo = ToTipo(node.Kind),
                    Cartao = ToCartao(node)
                });
            }

            var fios = new List<AskProgramEdgeDto>();
            var edges = snapshot.Edges ?? Array.Empty<ProgramGraphEdgeData>();
            for (var i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                if (!keptIds.Contains(edge.FromBlockId) || !keptIds.Contains(edge.ToBlockId))
                    continue;
                fios.Add(new AskProgramEdgeDto
                {
                    De = edge.FromBlockId,
                    Porta = ToPorta(edge.OutputPortIndex),
                    Para = edge.ToBlockId
                });
            }

            var entrada = snapshot.EntryBlockId;
            var tomadaLigada = !string.IsNullOrWhiteSpace(entrada) && keptIds.Contains(entrada);
            return new AskProgramDto
            {
                TomadaLigada = tomadaLigada,
                Entrada = tomadaLigada ? entrada : null,
                Blocos = blocos.ToArray(),
                Fios = fios.ToArray()
            };
        }

        public static AskProgramDto Empty()
        {
            return new AskProgramDto
            {
                TomadaLigada = false,
                Entrada = null,
                Blocos = Array.Empty<AskProgramBlockDto>(),
                Fios = Array.Empty<AskProgramEdgeDto>()
            };
        }

        static string ToTipo(ProgramBlockKind kind)
        {
            return kind switch
            {
                ProgramBlockKind.Move => TipoMovimento,
                ProgramBlockKind.Write => TipoMateriais,
                ProgramBlockKind.Condition => TipoCondicao,
                ProgramBlockKind.Accept => TipoAceitar,
                ProgramBlockKind.Reject => TipoRejeitar,
                _ => TipoMovimento
            };
        }

        static string ToPorta(int outputPortIndex)
        {
            return outputPortIndex switch
            {
                1 => PortaVerdadeiro,
                2 => PortaFalso,
                _ => PortaSaida
            };
        }

        static string ToCartao(ProgramGraphNodeData node)
        {
            if (node.DirectionCard == MoveDirection.Left)
                return CartaoEsquerda;
            if (node.DirectionCard == MoveDirection.Right)
                return CartaoDireita;
            if (node.SymbolCard == null)
                return null;
            return ItsBenchTapeCompact.ToFactoryToken(node.SymbolCard.Value);
        }
    }
}
