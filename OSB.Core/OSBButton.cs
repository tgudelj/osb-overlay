using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSB {
    /// <summary>
    /// On screen button definition
    /// </summary>
    public class OSBButton {
        public OSBButton()
        {
            X = 0;
            Y = 0;
            Width = 50;
            Height = 50;
            ImageOn = null;
            ImageOff = null;
            JoyBtnId = -1;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="x">Button left edge relative to form [pixels]</param>
        /// <param name="y">Button top edge relative to form [pixels]default 0</param>
        /// <param name="width">Button width [pixels]</param>
        /// <param name="height">Button height [pixels]</param>
        /// <param name="imageOff">Image to display when button is not pressed, optional</param>
        /// <param name="imageOn">Image to display when button is pressed, optional</param>
        public OSBButton(int x, int y, int width = 50, int height = 50, string imageOff = null, string imageOn = null, int joyBtnId = -1)
        {
            ImageOff = imageOff;
            ImageOn = imageOn;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            JoyBtnId = joyBtnId;
        }

        /// <summary>
        /// Virtual joystick button Id
        /// </summary>
        [JsonProperty("joyBtnId")]
        public int JoyBtnId { get; set; }

        /// <summary>
        /// Button image when button is not pressed
        /// </summary>
        [JsonProperty("imageOff")]
        public string ImageOff { get; set; }

        /// <summary>
        /// Button image when button is pressed
        /// </summary>
        [JsonProperty("imageOn")]
        public string ImageOn { get; set; }

        /// <summary>
        /// Button left edge relative to form [pixels]
        /// </summary>
        [JsonProperty("x")]
        public int X { get; set; }

        /// <summary>
        /// Button top edge relative to form [pixels]
        /// </summary>
        [JsonProperty("y")]
        public int Y { get; set; }

        /// <summary>
        /// Button width [pixels]
        /// </summary>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <summary>
        /// Button height [pixels]
        /// </summary>
        [JsonProperty("height")]
        public int Height { get; set; }
    }
}
