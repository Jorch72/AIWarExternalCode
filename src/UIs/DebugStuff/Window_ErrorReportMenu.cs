using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public class Window_ErrorReportMenu : WindowControllerAbstractBase
    {
        public static Window_ErrorReportMenu Instance;
        public Window_ErrorReportMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Engine_Universal.LastErrorText.Length <= 0 )
                return false;
            if ( this.IsPermanentlyClosed )
            {
                this.Close();
                return false;
            }
            return true;
        }
        
        private bool IsPermanentlyClosed;
        //private bool HasUpdatedSinceLastClose;

        public void Close()
        {
            Engine_Universal.LastErrorText = string.Empty;
            //this.HasUpdatedSinceLastClose = false;
        }

        public class bOpenLog : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Open Log" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Process.Start( Engine_Universal.CurrentPlayerDataDirectory + "ArcenDebugLog.txt" );
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bIgnore : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Ignore" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bIgnoreAndSuppress : ButtonAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShow( Buffer );
                Buffer.Add( "Ignore And Stop Reporting" );
            }
            public override MouseHandlingResult HandleClick()
            {
                Instance.IsPermanentlyClosed = true;
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class tText : TextAbstractBase
        {
            public override void GetTextToShow( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "Error Occurred:" );
                Buffer.Add( Engine_Universal.LastErrorText, 1024 );
            }
            public override void OnUpdate() { }
        }
    }
}