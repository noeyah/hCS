using System.Drawing.Drawing2D;
using Shared;

namespace TestClient;

public class GameForm : Form
{
	private const float MapMax = 100f;
	private const float CellSize = 10f;

	private readonly ClientSession _session;
	private readonly System.Windows.Forms.Timer _timer;
	private readonly Pen _cellPen;

	private bool _up;
	private bool _down;
	private bool _left;
	private bool _right;
	private MoveFlag _lastSent = MoveFlag.None;

	public GameForm(ClientSession session)
	{
		_session = session;

		Text = "hCS TestClient";
		ClientSize = new Size(600, 600);
		DoubleBuffered = true;
		KeyPreview = true;
		BackColor = Color.FromArgb(30, 30, 30);

		_cellPen = new Pen(Color.FromArgb(70, 70, 70))
		{
			DashStyle = DashStyle.Dash,
		};

		_timer = new System.Windows.Forms.Timer { Interval = 33 };
		_timer.Tick += (_, _) => Invalidate();
		_timer.Start();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_timer.Dispose();
			_cellPen.Dispose();
		}

		base.Dispose(disposing);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (SetKey(e.KeyCode, true))
		{
			SendMoveIfChanged();
		}
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		base.OnKeyUp(e);
		if (SetKey(e.KeyCode, false))
		{
			SendMoveIfChanged();
		}
	}

	private bool SetKey(Keys k, bool down)
	{
		switch (k)
		{
			case Keys.W:
			case Keys.Up:
				_up = down;
				return true;
			case Keys.S:
			case Keys.Down:
				_down = down;
				return true;
			case Keys.A:
			case Keys.Left:
				_left = down;
				return true;
			case Keys.D:
			case Keys.Right:
				_right = down;
				return true;
			default:
				return false;
		}
	}

	private void SendMoveIfChanged()
	{
		MoveFlag f = MoveFlag.None;
		if (_up)
		{
			f |= MoveFlag.Up;
		}
		if (_down)
		{
			f |= MoveFlag.Down;
		}
		if (_left)
		{
			f |= MoveFlag.Left;
		}
		if (_right)
		{
			f |= MoveFlag.Right;
		}

		if (f != _lastSent)
		{
			_lastSent = f;
			_session.SendMove(f);
		}
	}

	private void DrawCells(Graphics g, int ox, int oy, int size)
	{
		int count = (int)MathF.Ceiling(MapMax / CellSize);

		for (int i = 1; i < count; i++)
		{
			float offset = i * CellSize / MapMax * size;
			g.DrawLine(_cellPen, ox + offset, oy, ox + offset, oy + size);
			g.DrawLine(_cellPen, ox, oy + offset, ox + size, oy + offset);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;

		int size = Math.Min(ClientSize.Width, ClientSize.Height) - 40;
		int ox = 20;
		int oy = 20;

		DrawCells(g, ox, oy, size);
		g.DrawRectangle(Pens.Gray, ox, oy, size, size);

		foreach (KeyValuePair<int, PointF> kv in _session.Players)
		{
			PointF p = kv.Value;
			float sx = ox + p.X / MapMax * size;
			float sy = oy + (MapMax - p.Y) / MapMax * size;

			bool me = kv.Key == _session.MyUid;
			Brush brush = me ? Brushes.OrangeRed : Brushes.DeepSkyBlue;
			float r = me ? 8f : 6f;

			g.FillEllipse(brush, sx - r, sy - r, r * 2, r * 2);
		}

		g.DrawString(
			$"uid={_session.MyUid}  players={_session.Players.Count}  (WASD/화살표 이동)",
			Font, Brushes.White, ox, oy + size + 6);
	}
}
