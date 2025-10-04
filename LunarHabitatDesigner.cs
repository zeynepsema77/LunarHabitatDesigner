using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Versioning;

using System.Runtime.Versioning;
using System.Windows.Forms;

namespace LunarHabitatDesigner
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

        }


        public partial class MainForm : Form
        {
            private List<Room> level1Rooms = new List<Room>();
            private List<Room> level2Rooms = new List<Room>();
            private List<Room> level3Rooms = new List<Room>();
            private Room selectedRoom = null;
            private Point dragOffset;
            private int currentLevel = 1;
            private int crewSize = 2;
            private const int SCALE = 50;
            private Panel drawPanel;
            private ComboBox levelSelector;
            private ComboBox crewSelector;
            private Label infoLabel;
            private Label statsLabel;
            private Label pressureLabel;
            private Label warningLabel;

           
            private const float OPERATIONAL_PRESSURE = 14.7f; // psi
            private const float MAX_PRESSURE = 60.0f; // Test basıncı

            
            private Dictionary<string, List<string>> incompatibleRooms = new Dictionary<string, List<string>>
        {
            { "UWMS", new List<string> { "Galley Prep", "Galley Sort", "Medical", "Bio Lab", "Physics Lab", "Geo Lab", "Human Lab" } },
            { "Hygiene", new List<string> { "Galley Prep", "Galley Sort", "Medical", "Bio Lab", "Physics Lab", "Geo Lab", "Human Lab" } },
            { "Medical", new List<string> { "UWMS", "Hygiene", "Galley Prep", "Galley Sort", "Exercise" } },
            { "Galley Prep", new List<string> { "UWMS", "Hygiene", "Medical", "Sleep 1", "Sleep 2" } },
            { "Galley Sort", new List<string> { "UWMS", "Hygiene", "Medical", "Sleep 1", "Sleep 2" } },
            { "Airlock", new List<string> { "Sleep 1", "Sleep 2", "Medical", "Galley Prep", "Galley Sort" } }
        };

            public MainForm()
            {
                InitializeComponent();
                InitializeCustomComponents();
                InitializeRooms();
            }

            private void InitializeComponent()
            {
                this.SuspendLayout();
                this.ClientSize = new Size(1200, 850);
                this.Text = "Lunar Habitat Layout Designer - Seviye 1";
                this.BackColor = Color.FromArgb(20, 20, 30);
                this.DoubleBuffered = true;
                this.ResumeLayout(false);
            }

            private void InitializeCustomComponents()
            {
                
                drawPanel = new Panel
                {
                    Location = new Point(10, 120),
                    Size = new Size(1000, 700),
                    BackColor = Color.FromArgb(30, 30, 40),
                    BorderStyle = BorderStyle.FixedSingle
                };
                drawPanel.Paint += DrawPanel_Paint;
                drawPanel.MouseDown += DrawPanel_MouseDown;
                drawPanel.MouseMove += DrawPanel_MouseMove;
                drawPanel.MouseUp += DrawPanel_MouseUp;
                this.Controls.Add(drawPanel);

                
                Label levelLabel = new Label
                {
                    Text = "Seviye:",
                    Location = new Point(20, 20),
                    Size = new Size(60, 30),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                this.Controls.Add(levelLabel);

                levelSelector = new ComboBox
                {
                    Location = new Point(85, 20),
                    Size = new Size(150, 30),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 10)
                };
                levelSelector.Items.AddRange(new object[] {
                "Seviye 1 - Airlock & İş",
                "Seviye 2 - Hijyen & Egzersiz",
                "Seviye 3 - Yaşam Alanı"
            });
                levelSelector.SelectedIndex = 0;
                levelSelector.SelectedIndexChanged += LevelSelector_Changed;
                this.Controls.Add(levelSelector);

                
                statsLabel = new Label
                {
                    Location = new Point(250, 20),
                    Size = new Size(400, 30),
                    ForeColor = Color.Cyan,
                    Font = new Font("Segoe UI", 9),
                    Text = "",
                    AutoSize = false
                };
                this.Controls.Add(statsLabel);

                
                pressureLabel = new Label
                {
                    Location = new Point(250, 50),
                    Size = new Size(400, 30),
                    ForeColor = Color.Lime,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Text = "",
                    AutoSize = false
                };
                this.Controls.Add(pressureLabel);

                
                warningLabel = new Label
                {
                    Location = new Point(250, 80),
                    Size = new Size(550, 30),
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Text = "",
                    AutoSize = false
                };
                this.Controls.Add(warningLabel);

                
                infoLabel = new Label
                {
                    Location = new Point(1020, 120),
                    Size = new Size(160, 400),
                    ForeColor = Color.White,
                    Font = new Font("Consolas", 8),
                    Text = "KONTROLLER:\n\n" +
                           "• Oda seç: Tıkla\n" +
                           "• Taşı: Sürükle\n" +
                           "• Sil: [Del]\n\n" +
                           "UYARILAR:\n\n" +
                           "🔴 Kırmızı = Yanlış\n" +
                           "    yerleşim\n\n" +
                           "Hijyen/UWMS\n" +
                           "mutfak/labdan\n" +
                           "uzak olmalı!\n\n" +
                           "Airlock yaşam\n" +
                           "alanlarından\n" +
                           "ayrı olmalı!",
                    BackColor = Color.FromArgb(40, 40, 50),
                    Padding = new Padding(10)
                };
                this.Controls.Add(infoLabel);

                
                Button resetBtn = new Button
                {
                    Text = "Sıfırla",
                    Location = new Point(670, 20),
                    Size = new Size(100, 35),
                    BackColor = Color.FromArgb(200, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                resetBtn.Click += (s, e) => { InitializeRooms(); drawPanel.Invalidate(); };
                this.Controls.Add(resetBtn);

                Button addRoomBtn = new Button
                {
                    Text = "Yeni Oda",
                    Location = new Point(780, 20),
                    Size = new Size(100, 35),
                    BackColor = Color.FromArgb(50, 150, 200),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                addRoomBtn.Click += AddRoom_Click;
                this.Controls.Add(addRoomBtn);

                this.KeyPreview = true;
                this.KeyDown += MainForm_KeyDown;
            }

            private void InitializeRooms()
            {
                level1Rooms.Clear();
                level2Rooms.Clear();
                level3Rooms.Clear();

                
                float sleepMultiplier = crewSize / 2.0f;
                float wasteMultiplier = (crewSize > 2) ? 2.0f : 1.0f;
                float exerciseMultiplier = Math.Max(1, (int)Math.Ceiling(crewSize / 4.0f));
                float diningMultiplier = crewSize / 2.0f;

                
                // Minimum total area: 2 kişi için 42.63 m²

                // Level 1 - Airlock & EVA Operations Zone
                level1Rooms.Add(new Room("Airlock", 2, 2, 5.18f, Color.FromArgb(100, 150, 200)));
                level1Rooms.Add(new Room("Work Bench", 8, 2, 1.30f, Color.FromArgb(150, 100, 200)));
                level1Rooms.Add(new Room("EVA Computer", 12, 2, 0.97f, Color.FromArgb(200, 150, 100)));
                level1Rooms.Add(new Room("Geo Lab", 8, 5, 1.87f, Color.FromArgb(100, 200, 150)));
                level1Rooms.Add(new Room("Sub-Systems", 2, 6, 3.0f, Color.FromArgb(150, 150, 150)));
                level1Rooms.Add(new Room("Storage", 14, 2, 2.0f * sleepMultiplier, Color.FromArgb(180, 180, 100)));

                // Level 2 - Hygiene & Exercise Zone (UWMS ve Hygiene yan yana, lablardan uzak)
                level2Rooms.Add(new Room("UWMS", 2, 2, 1.04f * wasteMultiplier, Color.FromArgb(100, 150, 200)));
                level2Rooms.Add(new Room("Hygiene", 2, 4, 1.04f * wasteMultiplier, Color.FromArgb(150, 200, 200)));
                level2Rooms.Add(new Room("Exercise", 6, 2, 2.09f * exerciseMultiplier, Color.FromArgb(200, 100, 100)));
                level2Rooms.Add(new Room("Bio Lab", 10, 2, 2.5f, Color.FromArgb(100, 200, 100)));
                level2Rooms.Add(new Room("Sub-Systems", 14, 2, 4.0f, Color.FromArgb(150, 150, 150)));
                level2Rooms.Add(new Room("Core Access", 6, 6, 2.0f, Color.FromArgb(120, 120, 120)));

                // Level 3 - Living Area (Sleep, Dining, Labs - Medical ve Galley ayrı)
                int startX = 2;
                for (int i = 0; i < crewSize; i++)
                {
                    level3Rooms.Add(new Room($"Sleep {i + 1}", startX + (i * 2), 2, 1.85f, Color.FromArgb(100, 100, 200)));
                }

                level3Rooms.Add(new Room("Ward Table", 7, 2, 2.23f * diningMultiplier, Color.FromArgb(200, 150, 100)));
                level3Rooms.Add(new Room("Medical", 11, 2, 3.43f, Color.FromArgb(200, 100, 100)));
                level3Rooms.Add(new Room("Galley Prep", 2, 5, 1.17f, Color.FromArgb(150, 200, 100)));
                level3Rooms.Add(new Room("Galley Sort", 4, 5, 0.95f, Color.FromArgb(150, 200, 100)));
                level3Rooms.Add(new Room("Computer", 7, 5, 2.10f, Color.FromArgb(100, 200, 200)));
                level3Rooms.Add(new Room("Stretching", 2, 7, 2.68f * sleepMultiplier, Color.FromArgb(200, 200, 100)));
                level3Rooms.Add(new Room("Physics Lab", 11, 5, 2.5f, Color.FromArgb(200, 100, 200)));
                level3Rooms.Add(new Room("Human Lab", 15, 2, 2.0f, Color.FromArgb(150, 100, 150)));
                level3Rooms.Add(new Room("Storage", 15, 6, 3.0f, Color.FromArgb(180, 180, 100)));

                UpdateStats();
                CheckRoomProximity();
                drawPanel.Invalidate();
            }

            private void CrewSelector_Changed(object sender, EventArgs e)
            {
                crewSize = (crewSelector.SelectedIndex + 1) * 2; // 2, 4, veya 6
                this.Text = $"Lunar Habitat Layout Designer - {crewSize} Kişilik Mürettebat";
                InitializeRooms();
            }

            private void DrawPanel_Paint(object sender, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (Pen gridPen = new Pen(Color.FromArgb(50, 50, 60)))
                {
                    for (int i = 0; i <= 50; i++)
                    {
                        g.DrawLine(gridPen, i * SCALE, 0, i * SCALE, drawPanel.Height);
                        g.DrawLine(gridPen, 0, i * SCALE, drawPanel.Width, i * SCALE);
                    }
                }

                List<Room> currentRooms = GetCurrentLevelRooms();
                foreach (Room room in currentRooms)
                {
                    DrawRoom(g, room);
                }
            }

            private void DrawRoom(Graphics g, Room room)
            {
                Rectangle rect = new Rectangle(
                    room.X * SCALE,
                    room.Y * SCALE,
                    room.Width * SCALE,
                    room.Height * SCALE
                );

                Color roomColor = room.HasWarning ? Color.FromArgb(200, 50, 50) : room.Color;

                using (SolidBrush brush = new SolidBrush(roomColor))
                {
                    g.FillRectangle(brush, rect);
                }

                Color borderColor = room == selectedRoom ? Color.Yellow :
                                   room.HasWarning ? Color.Red : Color.White;
                int borderWidth = room.HasWarning ? 3 : 2;

                using (Pen pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawRectangle(pen, rect);
                }

                string displayText = $"{room.Name}\n{room.Area:F2}m²";
                if (room.HasWarning)
                    displayText += "\n⚠ UYARI!";

                using (Font font = new Font("Segoe UI", 8, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(displayText, font, textBrush, rect, sf);
                }
            }

            private void DrawPanel_MouseDown(object sender, MouseEventArgs e)
            {
                List<Room> rooms = GetCurrentLevelRooms();

                for (int i = rooms.Count - 1; i >= 0; i--)
                {
                    Room room = rooms[i];
                    Rectangle rect = new Rectangle(
                        room.X * SCALE,
                        room.Y * SCALE,
                        room.Width * SCALE,
                        room.Height * SCALE
                    );

                    if (rect.Contains(e.Location))
                    {
                        selectedRoom = room;
                        dragOffset = new Point(e.X - rect.X, e.Y - rect.Y);
                        UpdateInfoLabel();
                        drawPanel.Invalidate();
                        return;
                    }
                }

                selectedRoom = null;
                UpdateInfoLabel();
                drawPanel.Invalidate();
            }

            private void DrawPanel_MouseMove(object sender, MouseEventArgs e)
            {
                if (selectedRoom != null && e.Button == MouseButtons.Left)
                {
                    int newX = (e.X - dragOffset.X) / SCALE;
                    int newY = (e.Y - dragOffset.Y) / SCALE;

                    newX = Math.Max(0, Math.Min(newX, 50 - selectedRoom.Width));
                    newY = Math.Max(0, Math.Min(newY, 35 - selectedRoom.Height));

                    selectedRoom.X = newX;
                    selectedRoom.Y = newY;

                    CheckRoomProximity();
                    UpdateInfoLabel();
                    drawPanel.Invalidate();
                }
            }

            private void DrawPanel_MouseUp(object sender, MouseEventArgs e)
            {
                
            }

            private void MainForm_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Delete && selectedRoom != null)
                {
                    List<Room> rooms = GetCurrentLevelRooms();
                    rooms.Remove(selectedRoom);
                    selectedRoom = null;
                    UpdateStats();
                    CheckRoomProximity();
                    drawPanel.Invalidate();
                }
            }

            private void LevelSelector_Changed(object sender, EventArgs e)
            {
                currentLevel = levelSelector.SelectedIndex + 1;
                this.Text = $"Lunar Habitat Layout Designer - Seviye {currentLevel}";
                selectedRoom = null;
                UpdateStats();
                CheckRoomProximity();
                drawPanel.Invalidate();
            }

            private void AddRoom_Click(object sender, EventArgs e)
            {
                using (AddRoomDialog dialog = new AddRoomDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        Room newRoom = new Room(
                            dialog.RoomName,
                            2, 2,
                            dialog.RoomArea,
                            dialog.RoomColor
                        );
                        GetCurrentLevelRooms().Add(newRoom);
                        UpdateStats();
                        CheckRoomProximity();
                        drawPanel.Invalidate();
                    }
                }
            }

            private void CheckRoomProximity()
            {
                List<Room> rooms = GetCurrentLevelRooms();
                List<string> warnings = new List<string>();

                foreach (Room room in rooms)
                {
                    room.HasWarning = false;
                }

                foreach (Room room1 in rooms)
                {
                    if (!incompatibleRooms.ContainsKey(room1.Name))
                        continue;

                    foreach (Room room2 in rooms)
                    {
                        if (room1 == room2)
                            continue;

                        if (incompatibleRooms[room1.Name].Contains(room2.Name))
                        {
                            if (AreRoomsAdjacent(room1, room2))
                            {
                                room1.HasWarning = true;
                                room2.HasWarning = true;
                                warnings.Add($"{room1.Name} ↔ {room2.Name}");
                            }
                        }
                    }
                }

                if (warnings.Count > 0)
                {
                    warningLabel.Text = "⚠ UYARI: Yan yana olmamalı: " + string.Join(", ", warnings.Distinct());
                }
                else
                {
                    warningLabel.Text = "✓ Tüm odalar doğru konumlandırılmış";
                    warningLabel.ForeColor = Color.Lime;
                }
            }

            private bool AreRoomsAdjacent(Room r1, Room r2)
            {
                
                bool horizontalAdjacent =
                    (r1.X + r1.Width == r2.X || r2.X + r2.Width == r1.X) &&
                    !(r1.Y + r1.Height <= r2.Y || r2.Y + r2.Height <= r1.Y);

                
                bool verticalAdjacent =
                    (r1.Y + r1.Height == r2.Y || r2.Y + r2.Height == r1.Y) &&
                    !(r1.X + r1.Width <= r2.X || r2.X + r2.Width <= r1.X);

                return horizontalAdjacent || verticalAdjacent;
            }

            private List<Room> GetCurrentLevelRooms()
            {
                switch (currentLevel)
                {
                    case 1: return level1Rooms;
                    case 2: return level2Rooms;
                    case 3: return level3Rooms;
                    default: return level1Rooms;
                }
            }

            private void UpdateStats()
            {
                List<Room> rooms = GetCurrentLevelRooms();
                float totalArea = rooms.Sum(r => r.Area);
                int roomCount = rooms.Count;

                
                float pressureLoad = CalculatePressureLoad(totalArea);
                float safetyFactor = CalculateSafetyFactor(pressureLoad);

                statsLabel.Text = $"Seviye {currentLevel} - Alan: {totalArea:F2}m² | Oda: {roomCount}";

                Color pressureColor = safetyFactor >= 4.0f ? Color.Lime :
                                     safetyFactor >= 2.0f ? Color.Yellow : Color.Red;

                pressureLabel.ForeColor = pressureColor;
                pressureLabel.Text = $"Basınç Yükü: {pressureLoad:F1} lb/in | Güvenlik Faktörü: {safetyFactor:F2}x";
            }

            private float CalculatePressureLoad(float area)
            {
                
                float diameter = (float)Math.Sqrt(area / Math.PI) * 2;
                float circumference = diameter * (float)Math.PI;

                return (OPERATIONAL_PRESSURE * area) / circumference;
            }

            private float CalculateSafetyFactor(float currentLoad)
            {
                
                const float ULTIMATE_LOAD = 8700f; // lb/in
                return ULTIMATE_LOAD / (currentLoad > 0 ? currentLoad : 1);
            }

            private void UpdateInfoLabel()
            {
                if (selectedRoom != null)
                {
                    infoLabel.Text = $"SEÇİLİ ODA:\n" +
                                   $"{selectedRoom.Name}\n\n" +
                                   $"Alan: {selectedRoom.Area:F2}m²\n" +
                                   $"Boyut: {selectedRoom.Width}x{selectedRoom.Height}m\n" +
                                   $"Konum: ({selectedRoom.X}, {selectedRoom.Y})\n\n" +
                                   $"Durum:\n" +
                                   (selectedRoom.HasWarning ? "⚠ UYARI!\nYanlış yerleşim" : "✓ Normal") +
                                   $"\n\n[Del]: Sil";
                }
                else
                {
                    infoLabel.Text = "KONTROLLER:\n\n" +
                                   "• Oda seç: Tıkla\n" +
                                   "• Taşı: Sürükle\n" +
                                   "• Sil: [Del]\n\n" +
                                   "UYARILAR:\n\n" +
                                   "🔴 Kırmızı = Yanlış\n" +
                                   "    yerleşim\n\n" +
                                   "Hijyen/UWMS\n" +
                                   "mutfak/labdan\n" +
                                   "uzak olmalı!\n\n" +
                                   "Airlock yaşam\n" +
                                   "alanlarından\n" +
                                   "ayrı olmalı!";
                }
            }
        }

        public class Room
        {
            public string Name { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public float Area { get; set; }
            public Color Color { get; set; }
            public bool HasWarning { get; set; }

            public Room(string name, int x, int y, float area, Color color)
            {
                Name = name;
                X = x;
                Y = y;
                Area = area;
                Color = color;
                HasWarning = false;

                Width = (int)Math.Ceiling(Math.Sqrt(area));
                Height = (int)Math.Ceiling(area / Width);
            }
        }

        public class AddRoomDialog : Form
        {
            private TextBox nameBox;
            private NumericUpDown areaBox;
            private Button okButton;
            private Button cancelButton;
            private Panel colorPanel;

            public string RoomName { get; private set; }
            public float RoomArea { get; private set; }
            public Color RoomColor { get; private set; }

            public AddRoomDialog()
            {
                this.Text = "Yeni Oda Ekle";
                this.Size = new Size(350, 250);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterParent;
                this.MaximizeBox = false;
                this.MinimizeBox = false;

                Label nameLabel = new Label { Text = "Oda Adı:", Location = new Point(20, 20), Size = new Size(80, 20) };
                nameBox = new TextBox { Location = new Point(110, 20), Size = new Size(200, 20), Text = "Yeni Oda" };

                Label areaLabel = new Label { Text = "Alan (m²):", Location = new Point(20, 60), Size = new Size(80, 20) };
                areaBox = new NumericUpDown
                {
                    Location = new Point(110, 60),
                    Size = new Size(200, 20),
                    DecimalPlaces = 2,
                    Minimum = 0.5M,
                    Maximum = 20M,
                    Value = 2M,
                    Increment = 0.5M
                };

                Label colorLabel = new Label { Text = "Renk:", Location = new Point(20, 100), Size = new Size(80, 20) };
                colorPanel = new Panel
                {
                    Location = new Point(110, 100),
                    Size = new Size(50, 30),
                    BackColor = Color.FromArgb(100, 150, 200),
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };
                colorPanel.Click += (s, e) =>
                {
                    ColorDialog cd = new ColorDialog();
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        colorPanel.BackColor = cd.Color;
                    }
                };

                okButton = new Button
                {
                    Text = "Ekle",
                    Location = new Point(130, 160),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.OK
                };
                okButton.Click += (s, e) =>
                {
                    RoomName = nameBox.Text;
                    RoomArea = (float)areaBox.Value;
                    RoomColor = colorPanel.BackColor;
                };

                cancelButton = new Button
                {
                    Text = "İptal",
                    Location = new Point(220, 160),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.Cancel
                };

                this.Controls.AddRange(new Control[] {
                nameLabel, nameBox,
                areaLabel, areaBox,
                colorLabel, colorPanel,
                okButton, cancelButton
            });

                this.AcceptButton = okButton;
                this.CancelButton = cancelButton;
            }
        }
    }
}