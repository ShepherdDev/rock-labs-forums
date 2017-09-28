using System.IO;

using CommonMark;
using CommonMark.Formatters;
using CommonMark.Syntax;

namespace com.rocklabs.Forums.Support
{
    /// <summary>
    /// Custom formatter to add "max-width: 100%" to the image tags.
    /// </summary>
    public class MarkdownHtmlFormatter : HtmlFormatter
    {
        public MarkdownHtmlFormatter( TextWriter target, CommonMarkSettings settings )
            : base( target, settings )
        {
        }

        protected override void WriteInline( Inline inline, bool isOpening, bool isClosing, out bool ignoreChildNodes )
        {
            if ( inline.Tag == InlineTag.Image && !this.RenderPlainTextInlines.Peek() )
            {
                ignoreChildNodes = false;

                if ( isOpening )
                {
                    Write( "<img src=\"" );
                    var uriResolver = Settings.UriResolver;
                    if ( uriResolver != null )
                        WriteEncodedUrl( uriResolver( inline.TargetUrl ) );
                    else
                        WriteEncodedUrl( inline.TargetUrl );

                    Write( "\" style=\"max-width: 100%;\" alt=\"" );

                    if ( !isClosing )
                        RenderPlainTextInlines.Push( true );
                }

                if ( isClosing )
                {
                    // this.RenderPlainTextInlines.Pop() is done by the plain text renderer above.

                    Write( '\"' );
                    if ( inline.LiteralContent.Length > 0 )
                    {
                        Write( " title=\"" );
                        WriteEncodedHtml( inline.LiteralContent );
                        Write( '\"' );
                    }

                    if ( Settings.TrackSourcePosition )
                        WritePositionAttribute( inline );
                    Write( " />" );
                }
            }
            else
            {
                base.WriteInline( inline, isOpening, isClosing, out ignoreChildNodes );
            }
        }
    }
}
