using NUnit.Framework;
using Newtonsoft.Json;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;
using ITS;
using ITS.Protocol;

namespace EditModeTests
{
    public class ItsBenchTapeCompactTests
    {
        [Test]
        public void EmptyTape_AtOrigin_IsSingleVazio()
        {
            var tape = new SimulationTape(0);
            var dto = ItsBenchTapeCompact.FromSnapshot(tape.Snapshot());

            Assert.That(dto.Cells, Is.EqualTo(new[] { ItsBenchTapeCompact.Vazio }));
            Assert.That(dto.HeadOffset, Is.EqualTo(0));
        }

        [Test]
        public void EmptyTape_HeadMovedRight_KeepsOriginAndActualOffset()
        {
            var tape = new SimulationTape(0);
            tape.Move(MoveDirection.Right);
            tape.Move(MoveDirection.Right);
            var dto = ItsBenchTapeCompact.FromSnapshot(tape.Snapshot());

            Assert.That(dto.Cells, Is.EqualTo(new[]
            {
                ItsBenchTapeCompact.Vazio,
                ItsBenchTapeCompact.Vazio,
                ItsBenchTapeCompact.Vazio
            }));
            Assert.That(dto.HeadOffset, Is.EqualTo(2));
            Assert.That(tape.HeadIndex, Is.EqualTo(2));
        }

        [Test]
        public void EmptyTape_HeadMovedLeft_SpansBackToOrigin()
        {
            var tape = new SimulationTape(0);
            tape.Move(MoveDirection.Left);
            var dto = ItsBenchTapeCompact.FromSnapshot(tape.Snapshot());

            Assert.That(dto.Cells, Is.EqualTo(new[]
            {
                ItsBenchTapeCompact.Vazio,
                ItsBenchTapeCompact.Vazio
            }));
            Assert.That(dto.HeadOffset, Is.EqualTo(0));
        }

        [Test]
        public void Level1Main_PadsBlanksAndPlacesHeadOnLastOccupied()
        {
            var tape = new SimulationTape(2, Symbol.Gear, Symbol.Screw, Symbol.Nut);
            var dto = ItsBenchTapeCompact.FromSnapshot(tape.Snapshot());

            Assert.That(
                dto.Cells,
                Is.EqualTo(new[]
                {
                    ItsBenchTapeCompact.Vazio,
                    ItsBenchTapeCompact.Engrenagem,
                    ItsBenchTapeCompact.Parafuso,
                    ItsBenchTapeCompact.Porca,
                    ItsBenchTapeCompact.Vazio
                }));
            Assert.That(dto.HeadOffset, Is.EqualTo(3));
        }

        [Test]
        public void AskTapeDto_SerializesSnakeCase()
        {
            var tape = new SimulationTape(2, Symbol.Gear, Symbol.Screw, Symbol.Nut);
            var dto = ItsBenchTapeCompact.FromSnapshot(tape.Snapshot());
            var json = JsonConvert.SerializeObject(dto, ItsRestJson.Settings);
            Assert.That(json, Does.Contain("\"cells\""));
            Assert.That(json, Does.Contain("\"head_offset\""));
            Assert.That(json, Does.Contain("engrenagem"));
        }

        [Test]
        public void HeadLeftOfOccupiedSpan_ExtendsWindowPastHead()
        {
            var tape = new SimulationTape(0);
            tape.Move(MoveDirection.Right);
            tape.Move(MoveDirection.Right);
            tape.Move(MoveDirection.Right);
            tape.Move(MoveDirection.Right);
            tape.Move(MoveDirection.Right);
            tape.Write(Symbol.Gear);
            while (tape.HeadIndex > 0)
                tape.Move(MoveDirection.Left);

            var dto = ItsBenchTapeCompact.FromSnapshot(tape.Snapshot());

            Assert.That(dto.Cells[dto.HeadOffset], Is.EqualTo(ItsBenchTapeCompact.Vazio));
            Assert.That(dto.Cells, Does.Contain(ItsBenchTapeCompact.Engrenagem));
            Assert.That(dto.Cells[0], Is.EqualTo(ItsBenchTapeCompact.Vazio));
            Assert.That(dto.Cells[dto.Cells.Length - 1], Is.EqualTo(ItsBenchTapeCompact.Vazio));
            Assert.That(dto.HeadOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(dto.HeadOffset, Is.LessThan(dto.Cells.Length));
        }

        [Test]
        public void InteriorBlanksBetweenOccupiedCells_AreKept()
        {
            var tape = new SimulationTape(0, Symbol.Gear, Symbol.Blank, Symbol.Blank, Symbol.Nut);
            var dto = ItsBenchTapeCompact.FromSnapshot(tape.Snapshot());

            Assert.That(
                dto.Cells,
                Is.EqualTo(new[]
                {
                    ItsBenchTapeCompact.Vazio,
                    ItsBenchTapeCompact.Engrenagem,
                    ItsBenchTapeCompact.Vazio,
                    ItsBenchTapeCompact.Vazio,
                    ItsBenchTapeCompact.Porca,
                    ItsBenchTapeCompact.Vazio
                }));
            Assert.That(dto.HeadOffset, Is.EqualTo(1));
        }
    }
}
