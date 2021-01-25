using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tushino
{
    public class ReplayProcessor
    {
        static readonly IFormatProvider provider = CultureInfo.InvariantCulture;
        static readonly string[][] sides = {
            new [] { "WEST", "синие" },
            new [] { "EAST", "красные" },
            new [] { "GUER", "зеленые" },
            new [] { "CIV", "гражданские" },
        };
        static readonly string[] winnerMessages = new[] { "Winner: ", "Победа: " };
        static readonly string[] winnerAdminMessages = new[] {
            "Победа синих.",
            "Победа красных.",
            "Победа зелёных.",
            "Победа гражданских."
        };

        ReplayParser reader;
        Dictionary<int, Unit> units;
        Replay result;
        int currentTime;
        int prevTime;
        public ReplayProcessor(TextReader text)
        {
            reader = new ReplayParser(text);
        }

        public Replay ProcessReplay()
        {
            reader.Down();

            reader.Down();
            var island = reader.ReadString();
            var mission = reader.ReadString();
            var dateTime = DateTime.ParseExact(reader.ReadString(), "yyyy-MM-dd-HH-mm-ss", provider);
            reader.SkipElement();
            var ver = reader.ReadInt();
            if (ver != 5) return null;

            result = new Replay
            {
                Island = island,
                Mission = mission,
                Timestamp = dateTime,
                //Server = replayName.Substring(0, 2)
            };

            reader.Up();
            if (!reader.HasMoreElements) return null;

            units = new Dictionary<int, Unit>();
            PasreFrame0();
            prevTime = 0;
            ParseFrames();
            result.IsFinished = true;
            return result;
        }

        public Replay GetResult()
        {
            return result;
        }

        private void ParseFrames()
        {
            while (reader.HasMoreElements)
            {
                var killsInFrame = new List<Kill>();
                reader.Down();
                currentTime = reader.ReadInt();
                result.PlayTime = currentTime;
                ParseEvents(killsInFrame);
                ParseUnitsInFrame(killsInFrame, currentTime - prevTime);
                reader.Up();
                prevTime = currentTime;
            }
        }

        private void ParseEvents(List<Kill> killsInFrame)
        {
            if (!reader.HasMoreElements) return;
            reader.Down();
            while (reader.HasMoreElements)
            {
                ParseEvent(killsInFrame);
            }
            reader.Up();
        }

        private void ParseEvent(List<Kill> killsInFrame)
        {
            if (!reader.HasMoreElements) return;
            reader.Down();

            var eventType = reader.ReadInt();

            switch (eventType)
            {
                case 0: //Info
                    ParseInfo(reader.ReadString());
                    break;
                case 1: //Unit added
                    AddUnit();
                    break;
                case 2: //Vehile added
                    AddVehicle();
                    break;
                case 3: //Change name (in-out)
                    var e = new EnterExitEvent
                    {
                        Time = currentTime,
                        UnitId = reader.ReadInt()
                    };
                    var from = reader.ReadString();
                    var to = reader.ReadString();
                    if (from.StartsWith("~"))
                    {
                        e.IsEnter = true;
                        e.User = to;
                    }
                    else
                    {
                        e.IsEnter = false;
                        e.User = from;
                    }
                    result.Events.Add(e);
                    break;
                case 4: //Kill
                    var kill = new Kill()
                    {
                        Time = reader.ReadInt(),
                        KillerId = reader.ReadInt(),
                        TargetId = reader.ReadInt(),
                        Weapon = reader.ReadString(),
                        Ammo = reader.ReadString(),
                        Distance = reader.ReadDouble()
                    };
                    result.Kills.Add(kill);
                    killsInFrame.Add(kill);
                    break;
            }

            reader.Up();
        }

        private void ParseInfo(string v)
        {
            foreach (var winnerMessage in winnerMessages)
            {
                if (v.StartsWith(winnerMessage)) //Победа
                {
                    var side = v.Substring(winnerMessage.Length);
                    var sideIndex = Array.FindIndex(sides, s => s[0] == side);
                    result.WinnerSide = sideIndex;
                }
            }

            if (v.StartsWith("Миссия завершена админом:")) // Завершено админом
            {
                foreach (var winnerMessage in winnerAdminMessages)
                {
                    if (v.Contains(winnerMessage)) //Победа
                    {
                        result.WinnerSide = Array.IndexOf(winnerAdminMessages, winnerMessage);
                    }
                }
            }


            if (v.StartsWith("КС: ")) //КС
            {
                var commanders = v.Substring("КС: ".Length).Split(';');
                if (commanders.Last().Trim().StartsWith("ИА: ")) //Admin
                {
                    result.Admin = commanders.Last().Trim().Substring("ИА: ".Length);
                }
                foreach (var co in commanders)
                {
                    var p = co.Split('-');
                    if (p.Length == 2)
                    {
                        var side = p[0].Trim();
                        var com = p[1].Trim();
                        var sideIndex = Array.FindIndex(sides, s => s[1] == side);
                        switch (sideIndex)
                        {
                            case 0:
                                result.CommanderWest = com;
                                break;
                            case 1:
                                result.CommanderEast = com;
                                break;
                            case 2:
                                result.CommanderGuer = com;
                                break;
                        }
                    }
                }
            }
        }

        private void PasreFrame0()
        {
            reader.Down(); // frame 0
            reader.SkipElement();
            reader.Down(); //frame 0 events

            while (reader.HasMoreElements)
            {
                reader.Down();
                var eventType = reader.ReadInt();
                switch (eventType)
                {
                    case 1: //Unit added
                        AddUnit();
                        break;
                    case 2: //Vehile added
                        AddVehicle();
                        break;
                    case 3: //Change name (in)
                        var id = reader.ReadInt();
                        var from = reader.ReadString();
                        var to = reader.ReadString();

                        var e = new EnterExitEvent
                        {
                            Time = currentTime,
                            UnitId = id
                        };

                        if (from.StartsWith("~") && !to.StartsWith("~"))
                        {
                            e.IsEnter = true;
                            e.User = to;
                        }
                        else
                        {
                            e.IsEnter = false;
                            e.User = from;
                        }
                        result.Events.Add(e);

                        Unit unit = null;
                        if (units.TryGetValue(id, out unit))
                        {
                            units[id].Name = to;
                        }
                        break;
                }
                reader.Up();
            }
            reader.Up();
            reader.Up();
        }

        private void AddVehicle()
        {
            var v = new Unit
            {
                Id = reader.ReadInt(),
                Class = reader.ReadString(),
                Icon = reader.ReadString(),
                Side = reader.ReadInt(),
                IsVehicle = true
            };
            AddUnitToCollections(v);
        }

        private void AddUnit()
        {
            var u = new Unit
            {
                Id = reader.ReadInt(),
                Name = reader.ReadString(),
                Class = reader.ReadString(),
                Side = reader.ReadInt(),
                Icon = reader.ReadString(),
                Squad = reader.ReadString(),
                Title = reader.ReadString()
            };

            AddUnitToCollections(u);
        }

        private void AddUnitToCollections(Unit u)
        {
            if (units.ContainsKey(u.Id))
            {
                result.Units.Remove(units[u.Id]);
                units.Remove(u.Id);
            }
            units.Add(u.Id, u);
            result.Units.Add(u);
        }

        private void ParseUnitsInFrame(List<Kill> killsInFrame, int timeDiff)
        {
            while (reader.HasMoreElements)
            {
                reader.Down();
                var unitId = reader.ReadInt();
                reader.SkipElement();
                reader.SkipElement();
                reader.SkipElement();
                reader.SkipElement();
                var damage = reader.HasMoreElements ? reader.ReadDouble() : 0;
                var vehicleId = reader.HasMoreElements ? (int?)reader.ReadInt() : null;

                Unit unit = null;
                if (units.TryGetValue(unitId, out unit))
                {
                    unit.Damage = damage;
                    if (!unit.TimeOfDeath.HasValue && damage >= 1)
                    {
                        unit.TimeOfDeath = currentTime;
                    }
                    else
                    {
                        unit.VehicleOrDriverId = vehicleId;
                        if(vehicleId.HasValue)
                        {
                            unit.TimeInVehicle += timeDiff;
                        }
                        else
                        {
                            unit.TimeOnFoot += timeDiff;
                        }
                    }
                }

                foreach (var kill in killsInFrame.Where(k => k.KillerId == unitId)) kill.KillerVehicleId = vehicleId;
                reader.Up();

            }
        }
    }
}
