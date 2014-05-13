using System;
using Eto.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;

namespace Eto.Forms
{
	public class TableItem
	{
		public bool ScaleWidth { get; set; }

		public Control Control { get; set; }

		public TableItem()
		{
		}

		public TableItem(Control control, bool scaleWidth = false)
		{
			Control = control;
			ScaleWidth = scaleWidth;
		}

		public static implicit operator TableItem(Control control)
		{
			return new TableItem(control);
		}

		public static implicit operator TableItem(TableItem[] items)
		{
			return new TableItem(new TableLayout(items));
		}

		public static implicit operator TableItem(TableRow[] rows)
		{
			return new TableItem(new TableLayout(rows));
		}
	}

	public class TableRow
	{
		Collection<TableItem> items;

		public bool ScaleHeight { get; set; }

		public Collection<TableItem> Items
		{ 
			get { return items ?? (items = new Collection<TableItem>()); }
			set { items = value; }
		}

		public TableRow(params TableItem[] items)
		{
			Items = new Collection<TableItem>(items.ToList());
		}

		public TableRow(IEnumerable<TableItem> items)
		{
			Items = new Collection<TableItem>(items.ToList());
		}

		public static implicit operator TableRow(Control control)
		{
			return new TableRow(control);
		}
		public static implicit operator TableRow(TableItem[] items)
		{
			return new TableRow(items);
		}
	}

	[ContentProperty("Contents")]
	[Handler(typeof(TableLayout.IHandler))]
	public class TableLayout : Layout
	{
		new IHandler Handler { get { return (IHandler)base.Handler; } }

		Control[,] controls;
		Size cellSize;
		List<Control> children;
		public static Size DefaultSpacing = new Size(5, 5);
		public static Padding DefaultPadding = new Padding(5);

		public override IEnumerable<Control> Controls
		{
			get
			{
				return controls == null ? Enumerable.Empty<Control>() : controls.OfType<Control>();
			}
		}

		public List<Control> Contents
		{
			get
			{
				if (children == null)
					children = new List<Control>();
				return children;
			}
		}

		public Size CellSize
		{
			get { return cellSize; }
			set
			{
				if (controls != null)
					throw new InvalidOperationException("Can only set the cell size of a table once");
				cellSize = value;
				if (!cellSize.IsEmpty)
				{
					controls = new Control[cellSize.Width, cellSize.Height];
					Handler.CreateControl(cellSize.Width, cellSize.Height);
					Initialize();
				}
			}
		}
		#if !PCL
		[TypeConverter(typeof(Int32ArrayConverter))]
		#endif
		public int[] ColumnScale
		{
			set
			{
				for (int col = 0; col < CellSize.Width; col++)
				{
					SetColumnScale(col, false);
				}
				foreach (var col in value)
				{
					SetColumnScale(col);
				}
			}
			get
			{
				var vals = new List<int>();
				for (int col = 0; col < CellSize.Width; col++)
				{
					if (GetColumnScale(col))
						vals.Add(col);
				}
				return vals.ToArray();
			}
		}
		#if !PCL
		[TypeConverter(typeof(Int32ArrayConverter))]
		#endif
		public int[] RowScale
		{
			set
			{
				for (int row = 0; row < CellSize.Height; row++)
				{
					SetRowScale(row, false);
				}
				foreach (var row in value)
				{
					SetRowScale(row);
				}
			}
			get
			{
				var vals = new List<int>();
				for (int row = 0; row < CellSize.Height; row++)
				{
					if (GetRowScale(row))
						vals.Add(row);
				}
				return vals.ToArray();
			}
		}

		#region Attached Properties

		static readonly EtoMemberIdentifier LocationProperty = new EtoMemberIdentifier(typeof(TableLayout), "Location");

		public static Point GetLocation(Control control)
		{
			return control.Properties.Get<Point>(LocationProperty);
		}

		public static void SetLocation(Control control, Point value)
		{
			control.Properties[LocationProperty] = value;
			var layout = control.Parent as TableLayout;
			if (layout != null)
				layout.Move(control, value);
		}

		static readonly EtoMemberIdentifier ColumnScaleProperty = new EtoMemberIdentifier(typeof(TableLayout), "ColumnScale");

		public static bool GetColumnScale(Control control)
		{
			return control.Properties.Get<bool>(ColumnScaleProperty);
		}

		public static void SetColumnScale(Control control, bool value)
		{
			control.Properties[ColumnScaleProperty] = value;
		}

		static readonly EtoMemberIdentifier RowScaleProperty = new EtoMemberIdentifier(typeof(TableLayout), "RowScale");

		public static bool GetRowScale(Control control)
		{
			return control.Properties.Get<bool>(RowScaleProperty);
		}

		public static void SetRowScale(Control control, bool value)
		{
			control.Properties[RowScaleProperty] = value;
		}

		#endregion

		public static Control AutoSized(Control control, Padding? padding = null, bool centered = false)
		{
			var layout = new TableLayout(3, 3);
			layout.Padding = padding ?? Padding.Empty;
			layout.Spacing = Size.Empty;
			if (centered)
			{
				layout.SetColumnScale(0);
				layout.SetColumnScale(2);
				layout.SetRowScale(0);
				layout.SetRowScale(2);
			}
			layout.Add(control, 1, 1);
			return layout;
		}

		public TableLayout()
		{
		}

		public TableLayout(int width, int height)
			: this(new Size(width, height))
		{
		}

		public TableLayout(Size size)
		{
			this.CellSize = size;
		}

		public TableLayout(params TableRow[] rows)
		{
			Create(rows);
		}

		public TableLayout(IEnumerable<TableRow> rows)
		{
			Create(rows.ToArray());
		}

		void Create(TableRow[] rows)
		{
			var columnCount = rows.Max(r => r != null ? r.Items.Count : 0);
			CellSize = new Size(columnCount, rows.Length);
			int rowIndex = 0;
			foreach (var row in rows)
			{
				if (row != null)
				{
					for (int columnIndex = 0; columnIndex < row.Items.Count; columnIndex++)
					{
						var item = row.Items[columnIndex];
						if (item != null)
						{
							Add(item.Control, columnIndex, rowIndex);
							if (item.ScaleWidth)
								SetColumnScale(columnIndex);
						}
						else
							SetColumnScale(columnIndex);
					}
					if (row.ScaleHeight)
						SetRowScale(rowIndex);
				}
				else
					SetRowScale(rowIndex);
				rowIndex++;
			}
		}


		[Obsolete("Use constructor without generator instead")]
		public TableLayout(int width, int height, Generator generator = null)
			: this(new Size(width, height), generator)
		{
		}

		[Obsolete("Use constructor without generator instead")]
		public TableLayout(Size size, Generator generator = null)
			: base(generator, typeof(IHandler), false)
		{
			this.CellSize = size;
		}

		public void SetColumnScale(int column, bool scale = true)
		{
			Handler.SetColumnScale(column, scale);
		}

		public bool GetColumnScale(int column)
		{
			return Handler.GetColumnScale(column);
		}

		public void SetRowScale(int row, bool scale = true)
		{
			Handler.SetRowScale(row, scale);
		}

		public bool GetRowScale(int row)
		{
			return Handler.GetRowScale(row);
		}

		public void Add(Control control, int x, int y)
		{
			if (control != null)
				control.Properties[LocationProperty] = new Point(x, y);
			InnerAdd(control, x, y);
		}

		void InnerAdd(Control control, int x, int y)
		{
			var old = controls[x, y];
			if (old != null)
				RemoveParent(old);
			controls[x, y] = control;
			if (control != null)
			{
				SetParent(control, () => Handler.Add(control, x, y));
			}
			else
			{
				Handler.Add(null, x, y);
			}
		}

		public void Add(Control child, int x, int y, bool xscale, bool yscale)
		{
			child.Properties[LocationProperty] = new Point(x, y);
			SetColumnScale(x, xscale);
			SetRowScale(y, yscale);
			Add(child, x, y);
		}

		public void Add(Control child, Point p)
		{
			Add(child, p.X, p.Y);
		}

		public void Move(Control child, int x, int y)
		{
			var index = controls.IndexOf(child);
			if (index != null)
				controls[index.Item1, index.Item2] = null;

			var old = controls[x, y];
			if (old != null)
				RemoveParent(old);
			controls[x, y] = child;
			child.Properties[LocationProperty] = new Point(x, y);
			Handler.Move(child, x, y);
		}

		public void Move(Control child, Point p)
		{
			Move(child, p.X, p.Y);
		}

		public override void Remove(Control child)
		{
			var index = controls.IndexOf(child);
			if (index != null)
			{
				controls[index.Item1, index.Item2] = null;
				Handler.Remove(child);
				RemoveParent(child);
			}
		}

		public Size Spacing
		{
			get { return Handler.Spacing; }
			set { Handler.Spacing = value; }
		}

		public Padding Padding
		{
			get { return Handler.Padding; }
			set
			{
				Handler.Padding = value;
			}
		}

		[OnDeserialized]
		void OnDeserialized(StreamingContext context)
		{
			OnDeserialized();
		}

		public override void EndInit()
		{
			base.EndInit();
			OnDeserialized(Parent != null); // mono calls EndInit BEFORE setting to parent
		}

		void OnDeserialized(bool direct = false)
		{
			if (Loaded || direct)
			{
				if (children != null)
				{
					foreach (var control in children)
					{
						var location = GetLocation(control);
						Add(control, location);
						if (GetColumnScale(control))
							SetColumnScale(location.X);
						if (GetRowScale(control))
							SetRowScale(location.Y);
					}
				}
			}
			else
			{
				PreLoad += HandleDeserialized;
			}
		}

		void HandleDeserialized(object sender, EventArgs e)
		{
			OnDeserialized(true);
			PreLoad -= HandleDeserialized;
		}

		[AutoInitialize(false)]
		public new interface IHandler : Layout.IHandler, IPositionalLayoutHandler
		{
			void CreateControl(int cols, int rows);

			bool GetColumnScale(int column);

			void SetColumnScale(int column, bool scale);

			bool GetRowScale(int row);

			void SetRowScale(int row, bool scale);

			Size Spacing { get; set; }

			Padding Padding { get; set; }
		}

		public static implicit operator TableLayout(TableRow[] rows)
		{
			return new TableLayout(rows);
		}
	}
}
