﻿using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Gui.Controls;
using MonoGame.Extended.Gui.Serialization;
using Newtonsoft.Json;

namespace MonoGame.Extended.Gui
{
    public class ScreenRoot : Control { }

    public class Screen : Element<GuiSystem>, IDisposable
    {
        public Screen()
        {
            Controls = new ControlCollection { ItemAdded = c => _isLayoutRequired = true };
            Windows = new WindowCollection(this) { ItemAdded = w => _isLayoutRequired = true };
        }

        public virtual void Dispose()
        {
        }

        [JsonProperty(Order = 1)]
        public ControlCollection Controls { get; set; }

        [JsonIgnore]
        public WindowCollection Windows { get; }

        public new float Width { get; private set; }
        public new float Height { get; private set; }
        public new Size2 Size => new Size2(Width, Height);
        public bool IsVisible { get; set; } = true;

        [JsonIgnore]
        public bool IsLayoutRequired { get { return _isLayoutRequired || Controls.Any(x => x.IsLayoutRequired); } }
        private bool _isLayoutRequired;

        public virtual void Update(GameTime gameTime) { }

        public void Show()
        {
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public T FindControl<T>(string name)
            where T : Control
        {
            return FindControl<T>(Controls, name);
        }

        private static T FindControl<T>(ControlCollection controls, string name)
            where T : Control
        {
            foreach (var control in controls)
            {
                if (control.Name == name)
                    return control as T;

                if (control.Controls.Any())
                {
                    var childControl = FindControl<T>(control.Controls, name);

                    if (childControl != null)
                        return childControl;
                }
            }

            return null;
        }

        public void Layout(IGuiContext context, Rectangle rectangle)
        {
            Width = rectangle.Width;
            Height = rectangle.Height;

            foreach (var control in Controls)
                LayoutHelper.PlaceControl(context, control, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

            foreach (var window in Windows)
                LayoutHelper.PlaceWindow(context, window, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

            _isLayoutRequired = false;
            foreach (var control in Controls) control.IsLayoutRequired = false;
        }

        public override void Draw(IGuiContext context, IGuiRenderer renderer, float deltaSeconds)
        {
            renderer.DrawRectangle(BoundingRectangle, Color.Green);
        }

        public static Screen FromStream(ContentManager contentManager, Stream stream, params Type[] customControlTypes)
        {
            return FromStream<Screen>(contentManager, stream, customControlTypes);
        }

        public static TScreen FromStream<TScreen>(ContentManager contentManager, Stream stream, params Type[] customControlTypes)
            where TScreen : Screen
        {
            var skinService = new SkinService();
            var serializer = new GuiJsonSerializer(contentManager, customControlTypes)
            {
                Converters =
                {
                    new SkinJsonConverter(contentManager, skinService, customControlTypes),
                    new ControlJsonConverter(skinService, customControlTypes)
                }
            };

            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var screen = serializer.Deserialize<TScreen>(jsonReader);
                return screen;
            }
        }

        public static Screen FromFile(ContentManager contentManager, string path, params Type[] customControlTypes)
        {
            using (var stream = TitleContainer.OpenStream(path))
            {
                return FromStream<Screen>(contentManager, stream, customControlTypes);
            }
        }

        public static TScreen FromFile<TScreen>(ContentManager contentManager, string path, params Type[] customControlTypes)
            where TScreen : Screen
        {
            using (var stream = TitleContainer.OpenStream(path))
            {
                return FromStream<TScreen>(contentManager, stream, customControlTypes);
            }
        }
    }
}