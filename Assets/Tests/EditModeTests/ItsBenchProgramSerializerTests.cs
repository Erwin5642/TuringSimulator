using System;
using NUnit.Framework;
using TuringSimulator.Core.ProgramGraph;
using TuringSimulator.Core.Types;
using ITS;

namespace EditModeTests
{
    public class ItsBenchProgramSerializerTests
    {
        [Test]
        public void SerializesFactoryNamesAndPorts()
        {
            var snap = new ProgramGraphSnapshot(
                new[]
                {
                    new ProgramGraphNodeData("m1", ProgramBlockKind.Move, null, MoveDirection.Left),
                    new ProgramGraphNodeData("c1", ProgramBlockKind.Condition, Symbol.Gear),
                    new ProgramGraphNodeData("w1", ProgramBlockKind.Write, Symbol.Nut),
                    new ProgramGraphNodeData("a1", ProgramBlockKind.Accept),
                    new ProgramGraphNodeData("orphan", ProgramBlockKind.Reject),
                },
                new[]
                {
                    new ProgramGraphEdgeData("m1", 0, "c1"),
                    new ProgramGraphEdgeData("c1", 1, "w1"),
                    new ProgramGraphEdgeData("c1", 2, "a1"),
                },
                "m1");

            var dto = ItsBenchProgramSerializer.FromSnapshot(snap);

            Assert.That(dto.TomadaLigada, Is.True);
            Assert.That(dto.Entrada, Is.EqualTo("m1"));
            Assert.That(dto.Blocos.Length, Is.EqualTo(5));
            Assert.That(dto.Blocos[0].Cartao, Is.EqualTo(ItsBenchProgramSerializer.CartaoEsquerda));
            Assert.That(dto.Blocos[1].Tipo, Is.EqualTo(ItsBenchProgramSerializer.TipoCondicao));
            Assert.That(dto.Blocos[1].Cartao, Is.EqualTo(ItsBenchTapeCompact.Engrenagem));
            Assert.That(dto.Fios[0].Porta, Is.EqualTo(ItsBenchProgramSerializer.PortaSaida));
            Assert.That(dto.Fios[1].Porta, Is.EqualTo(ItsBenchProgramSerializer.PortaVerdadeiro));
            Assert.That(dto.Fios[2].Porta, Is.EqualTo(ItsBenchProgramSerializer.PortaFalso));
        }

        [Test]
        public void EmptyEntry_IsTomadaSolta()
        {
            var snap = new ProgramGraphSnapshot(
                Array.Empty<ProgramGraphNodeData>(),
                Array.Empty<ProgramGraphEdgeData>(),
                string.Empty);

            var dto = ItsBenchProgramSerializer.FromSnapshot(snap);

            Assert.That(dto.TomadaLigada, Is.False);
            Assert.That(dto.Entrada, Is.Null);
            Assert.That(dto.Blocos, Is.Empty);
            Assert.That(dto.Fios, Is.Empty);
        }
    }
}
