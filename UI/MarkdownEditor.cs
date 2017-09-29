using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;

namespace com.rocklabs.Forums.UI
{
    /// <summary>
    /// Provides a control for editing markdown with built-in file upload and
    /// preview functionality.
    /// </summary>
    public class MarkdownEditor : CompositeControl
    {
        #region Protected Fields

        /// <summary>
        /// Contains the identifiers of the uploaded files.
        /// </summary>
        protected HiddenField _uploadedFiles;

        /// <summary>
        /// Contains the base URL to use when uploading files.
        /// </summary>
        protected HiddenField _publicApplicationRoot;

        /// <summary>
        /// Contains the BinaryFileTyoe GUID to use when uploading files.
        /// </summary>
        protected HiddenField _fileTypeGuid;

        /// <summary>
        /// Placeholder for the markdown text entered by the user.
        /// </summary>
        protected TextBox _text;

        #endregion

        #region Public Properties

        /// <summary>
        /// The raw markdown text entered by the user.
        /// </summary>
        public string Text
        {
            get
            {
                EnsureChildControls();

                return _text.Text;
            }
            set
            {
                EnsureChildControls();

                _text.Text = value;
            }
        }

        /// <summary>
        /// A Guid that represents the BinaryFileType to be used when uploading files. Leave
        /// null to disallow file uploading.
        /// </summary>
        public Guid? BinaryFileTypeGuid
        {
            get
            {
                EnsureChildControls();

                return _fileTypeGuid.Value.AsGuidOrNull();
            }
            set
            {
                EnsureChildControls();

                _fileTypeGuid.Value = value.ToStringSafe();
            }
        }

        /// <summary>
        /// The base URL to use when creating the links for uploaded files.
        /// </summary>
        public string PublicApplicationRoot
        {
            get
            {
                EnsureChildControls();

                return _publicApplicationRoot.Value;
            }
            set
            {
                EnsureChildControls();

                _publicApplicationRoot.Value = value;
            }
        }

        /// <summary>
        /// A list of identifiers of the BinaryFiles that were uploaded by the user.
        /// </summary>
        public List<int> UploadedFileIds
        {
            get
            {
                EnsureChildControls();

                return _uploadedFiles.Value
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( s => s.AsInteger() )
                    .Where( s => s != 0 )
                    .ToList();
            }
            set
            {
                EnsureChildControls();

                if ( value != null )
                {
                    _uploadedFiles.Value = string.Join( ",", value );
                }
                else
                {
                    _uploadedFiles.Value = string.Empty;
                }
            }
        }

        #endregion

        /// <summary>
        /// Creates all the child controls needed for this control.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            _uploadedFiles = new HiddenField();
            _uploadedFiles.ID = this.ID + "_uploadedFiles";
            Controls.Add( _uploadedFiles );

            _publicApplicationRoot = new HiddenField();
            _publicApplicationRoot.ID = this.ID + "_publicApplicationRoot";
            Controls.Add( _publicApplicationRoot );

            _fileTypeGuid = new HiddenField();
            _fileTypeGuid.ID = this.ID + "_fileTypeGuid";
            Controls.Add( _fileTypeGuid );

            _text = new TextBox();
            _text.ID = this.ID + "_text";
            _text.TextMode = TextBoxMode.MultiLine;

            Controls.Add( _text );
        }

        /// <summary>
        /// Render the comment into the web page.
        /// </summary>
        /// <param name="writer">The HtmlTextWriter to use when generating content.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( this.Visible )
            {
                if ( !string.IsNullOrWhiteSpace( CssClass ) )
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, CssClass );
                }

                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    _uploadedFiles.RenderControl( writer );
                    _fileTypeGuid.RenderControl( writer );
                    _publicApplicationRoot.RenderControl( writer );
                    _text.RenderControl( writer );
                }
                writer.RenderEndTag();

                //
                // Setup the Javascript to initialize the markdown editor.
                //
                string script = string.Format( @"
;(function ($)
{{
    function preview(content, callback)
    {{
        $.ajax({{
            url: '/api/RockLabs_Forums_Utility/MarkdownToHtml',
            type: 'POST',
            dataType: 'json',
            contentType: 'application/json',
            data: JSON.stringify(content)
        }})
        .done(function (result)
        {{
            callback(result);
        }});
    }}

    function uploadHelper(uploadedFile)
    {{
        var imageChar = '';
        if ((/\.(gif|jpg|jpeg|tif|tiff|png)$/i).test(uploadedFile.FileName))
        {{
            imageChar = '!';
        }}

        var $hfUploadedFiles = $('#{0}');
        $hfUploadedFiles.val($hfUploadedFiles.val() + ',' + uploadedFile.Id);

        return imageChar + '[' + uploadedFile.FileName + '](' + $('#{1}').val() + 'GetFile.ashx?Id=' + uploadedFile.Id + ')';
    }}

    $('#{3}').markdownEditor({{
        height: '',
        fullscreen: false,
        preview: true,
        onPreview: preview,
        imageUpload: $('#{2}').val() != '',
        uploadPath: '/FileUploader.ashx?isBinaryFile=1&fileTypeGuid=' + $('#{2}').val(),
        uploadHelper: uploadHelper
    }});
}})(jQuery);
",
                _uploadedFiles.ClientID,
                _publicApplicationRoot.ClientID,
                _fileTypeGuid.ClientID,
                _text.ClientID );

                ScriptManager.RegisterStartupScript( this, GetType(), "initialize", script, true );
            }
        }
    }
}
