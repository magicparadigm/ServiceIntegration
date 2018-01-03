using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Deserializers;
using RestSharp.Extensions;
using RestSharp.Serializers;
using RestSharp.Validation;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Collections;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;

public partial class ServiceIntegration : System.Web.UI.Page
{
    protected String JWT;
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            AuthInfo.Visible = true;
            button.Visible = true;
            button.InnerText = "Next";
            primarySignerSection.Visible = false;
            templates.Visible = false;
            button2.InnerText = "Submit";
            button2.Visible = false;
            uploadButton.InnerText = "Upload";
        }

        // Add event handlers for the navigation button on each of the wizard pages 
        PrefillClick.ServerClick += new EventHandler(prefill_Click);
        button.ServerClick += new EventHandler(button_Click);
        button2.ServerClick += new EventHandler(button2_Click);
        uploadButton.ServerClick += new EventHandler(uploadButton_Click);

    }

    protected void prefill_Click(object sender, EventArgs e)
    {
        // Default values 
        integratorKey.Value = "1a971ea6-780f-49fd-a622-d23232a01d52";
        userID.Value = "e602a60e-5ac0-44a5-9e2c-01e251ddf116";

        pKey.Value = File.ReadAllText(Server.MapPath("~/App_Data/" + @"pkey.txt"));
        
    }

    protected void button_Click(object sender, EventArgs e)
    {
        // Get a JWT
//        Session["JWT"] = createJWT();
        AuthInfo.Visible = false;
        button.Visible = false;
        primarySignerSection.Visible = true;
        templates.Visible = true;
        button2.Visible = true;
        
    }

    protected void button2_Click(object sender, EventArgs e)
    {
        primarySignerSection.Visible = false;
        templates.Visible = false;
        button2.Visible = false;
        createEnvelope();
    }

    protected String createJWT()
    {
        // Check to see whether we have a cached token that has not expired that we can uSe
        DateTime dsExpire;
        if ((Session["dsTokenExpire"] != null) && (Session["dsJWT"] != null))
            if ((!Session["dsTokenExpire"].Equals("") && (!Session["dsJWT"].Equals(""))))
            {
                dsExpire = (DateTime)Session["dstokenExpire"];
                if (DateTime.Now.Ticks < (0.75 * dsExpire.Ticks))
                {
                    return Session["dsJWT"].ToString();
                }
            }
        Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        rsa = DecodeRSAPrivateKey(FromBase64Url(pKey.Value));

        var bytes = rsa.ExportCspBlob(false);

        Microsoft.IdentityModel.Tokens.RsaSecurityKey _signingKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa);
        Microsoft.IdentityModel.Tokens.SigningCredentials signingCredentials =  new Microsoft.IdentityModel.Tokens.SigningCredentials(_signingKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256);

        var header = new JwtHeader(signingCredentials);
        header.Remove("alg");
        header.Add("alg", "RS256");


        // iss - integrator key
        // sub - the user ID (instead of DocuSign user name)

        var payload = new JwtPayload
            {
                { "iss", integratorKey.Value},
                {"sub", userID.Value},
                {"scope", "signature impersonation"},
                {"aud", "account-d.docusign.com"},
                {"iat", unixTimestamp },
                {"exp", unixTimestamp + 3600}
            };
        var secToken = new JwtSecurityToken(header, payload);
        secToken.SigningKey = _signingKey;

        var handler = new JwtSecurityTokenHandler();
        String dsTokenString = handler.WriteToken(secToken);

        string contentType = "application/x-www-form-urlencoded";
        string grantType = "grant_type=urn:ietf:params:oauth:grant-type:jwt-bearer";

        var client = new RestClient("https://account-d.docusign.com");
        var request = new RestRequest("/oauth/token", Method.POST);
        request.AddHeader("Content-Type", contentType);
        request.AddParameter("application/x-www-form-urlencoded", string.Format(grantType + "&assertion={0}", dsTokenString), ParameterType.RequestBody);

        IRestResponse response = client.Execute(request);

        JObject o = JObject.Parse(response.Content);
        String dsToken = (string)o["access_token"];
        String dsTokenType = (string)o["token_type"];
        int seconds = (int)o["expires_in"];
        DateTime dsTokenExpire = (DateTime)DateTime.Now.AddSeconds(seconds);

        // Save current time + token expiration time to calculate whether we should provision a new token or use cached token
        Session["dsTokenExpire"] = dsTokenExpire;

        // Optional code that can be used to find out DS accounts that can be used with the JWT. The base DS URL can also be derived using this call
        //------------------------------------------------------------------------------------------------------------------------------------------------------
        //var userInfo = new RestClient("https://account-d.docusign.com");
        //var userRequest = new RestRequest("/oauth/userinfo", Method.GET);
        //userRequest.AddHeader("content-type", "application/json");
        //userRequest.AddHeader("Authorization", "Bearer " + dsToken);
        //IRestResponse userResponse = userInfo.Execute(userRequest);

        //JObject i = JObject.Parse(userResponse.Content);

        //JToken accts = i.SelectToken("accounts");

        //foreach (var x in accts)
        //{
        //    if (x.SelectToken("is_default") != null)
        //    {
        //        String dsAccountId = (string)x.SelectToken("account_id");
        //        String dsBaseUrl = (string)x.SelectToken("base_uri");
        //        dsBaseUrl = dsBaseUrl + "/restapi";
        //    }
        //}
        //------------------------------------------------------------------------------------------------------------------------------------------------------

        Session["dsJWT"] = dsToken;
        return dsToken;
    }

    private static byte[] FromBase64Url(string base64Url)
    {
        string base64 = string.Empty;
        if (!string.IsNullOrEmpty(base64Url))
        {
            string padded = base64Url.Length % 4 == 0 ? base64Url : base64Url + "====".Substring(base64Url.Length % 4);
            base64 = padded.Replace("_", "/").Replace("-", "+");
        }
        return Convert.FromBase64String(base64);
    }


    private RSACryptoServiceProvider DecodeRSAPrivateKey(byte[] privkey)
    {
        byte[] MODULUS, E, D, P, Q, DP, DQ, IQ;

        // ---------  Set up stream to decode the asn.1 encoded RSA private key  ------
        System.IO.MemoryStream mem = new System.IO.MemoryStream(privkey);
        System.IO.BinaryReader binr = new System.IO.BinaryReader(mem);    //wrap Memory Stream with BinaryReader for easy reading
        byte bt = 0;
        ushort twobytes = 0;
        int elems = 0;
        try
        {
            twobytes = binr.ReadUInt16();
            if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                binr.ReadByte();        //advance 1 byte
            else if (twobytes == 0x8230)
                binr.ReadInt16();       //advance 2 bytes
            else
                return null;

            twobytes = binr.ReadUInt16();
            if (twobytes != 0x0102) //version number
                return null;
            bt = binr.ReadByte();
            if (bt != 0x00)
                return null;


            //------  all private key components are Integer sequences ----
            elems = GetIntegerSize(binr);
            MODULUS = binr.ReadBytes(elems);

            elems = GetIntegerSize(binr);
            E = binr.ReadBytes(elems);

            elems = GetIntegerSize(binr);
            D = binr.ReadBytes(elems);

            elems = GetIntegerSize(binr);
            P = binr.ReadBytes(elems);

            elems = GetIntegerSize(binr);
            Q = binr.ReadBytes(elems);

            elems = GetIntegerSize(binr);
            DP = binr.ReadBytes(elems);

            elems = GetIntegerSize(binr);
            DQ = binr.ReadBytes(elems);

            elems = GetIntegerSize(binr);
            IQ = binr.ReadBytes(elems);



            // ------- create RSACryptoServiceProvider instance and initialize with public key -----
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            RSAParameters RSAparams = new RSAParameters();
            RSAparams.Modulus = MODULUS;
            RSAparams.Exponent = E;
            RSAparams.D = D;
            RSAparams.P = P;
            RSAparams.Q = Q;
            RSAparams.DP = DP;
            RSAparams.DQ = DQ;
            RSAparams.InverseQ = IQ;
            RSA.ImportParameters(RSAparams);
            return RSA;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            binr.Close();
        }
    }

    private int GetIntegerSize(System.IO.BinaryReader binr)
    {
        byte bt = 0;
        byte lowbyte = 0x00;
        byte highbyte = 0x00;
        int count = 0;
        bt = binr.ReadByte();
        if (bt != 0x02)     //expect integer
            return 0;
        bt = binr.ReadByte();

        if (bt == 0x81)
            count = binr.ReadByte();    // data size in next byte
        else
            if (bt == 0x82)
        {
            highbyte = binr.ReadByte(); // data size in next 2 bytes
            lowbyte = binr.ReadByte();
            byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
            count = BitConverter.ToInt32(modint, 0);
        }
        else
        {
            count = bt;     // we already have the data size
        }



        while (binr.ReadByte() == 0x00)
        {   //remove high order zeros in data
            count -= 1;
        }
        binr.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);     //last     ReadByte wasn't a removed zero, so back up a byte
        return count;
    }

    protected void uploadButton_Click(object sender, EventArgs e)
    {
        try
        {
            if (FileUpload1.HasFile)
            {
                String filename = Path.GetFileName(FileUpload1.FileName);
                FileUpload1.SaveAs(Server.MapPath("~/App_Data/") + filename);
                uploadFile.Value = filename;
            }
        }
        catch (Exception ex)
        {
            uploadFile.Value = "Upload status: The file could not be uploaded. The following error occured: " + ex.Message;
        }
    }

    protected String RandomizeClientUserID()
    {
        Random random = new Random();

        return (random.Next()).ToString();
    }

    public class TextTab
    {
        public string tabLabel { get; set; }
        public string value { get; set; }
    }

    public class Tabs
    {
        public List<TextTab> textTabs { get; set; }
        public List<SignHereTab> signHereTabs { get; set; }
        public List<DateSignedTab> dateSignedTabs { get; set; }
    }

    public class CertifiedDelivery
    {

        public string email { get; set; }
        public string name { get; set; }
        public int recipientId { get; set; }
        public string roleName { get; set; }
        public string routingOrder { get; set; }
        public string clientUserId { get; set; }


    }

    public class Recipients
    {
        public List<CertifiedDelivery> certifiedDeliveries { get; set; }
    }

    public class Document
    {
        public string documentId { get; set; }
        public string name { get; set; }
        public string transformPdfFields { get; set; }

    }

    public class InlineTemplate
    {
        public string sequence { get; set; }
        public Recipients recipients { get; set; }

        public List<Document> documents { get; set; }
    }

    public class ServerTemplate
    {
        public string sequence { get; set; }
        public string templateId { get; set; }
    }

    public class CompositeTemplate
    {
        public string compositeTemplateId { get; set; }
        public List<InlineTemplate> inlineTemplates { get; set; }
        public List<ServerTemplate> serverTemplates { get; set; }
        public Document document { get; set; }
    }

    public class CreateEnvelopeRequest
    {
        public string status { get; set; }
        public string emailSubject { get; set; }
        public string emailBlurb { get; set; }
        public List<CompositeTemplate> compositeTemplates { get; set; }
    }
    public class CreateEnvelopeResponse
    {
        public string envelopeId { get; set; }
        public string uri { get; set; }
        public string statusDateTime { get; set; }
        public string status { get; set; }
    }

    public class SignHereTab
    {
        public string tabId { get; set; }
        public string name { get; set; }
        public string pageNumber { get; set; }
        public string documentId { get; set; }
        public string yPosition { get; set; }
        public string xPosition { get; set; }

        public string anchorString { get; set; }
        public string anchorXOffset { get; set; }
        public string anchorYOffset { get; set; }
        public string anchorIgnoreIfNotPresent { get; set; }
        public string anchorUnits { get; set; }
    }

    public class DateSignedTab
    {
        public string tabId { get; set; }
        public string name { get; set; }
        public string pageNumber { get; set; }
        public string documentId { get; set; }
        public string yPosition { get; set; }
        public string xPosition { get; set; }
        public string anchorString { get; set; }
        public string anchorXOffset { get; set; }
        public string anchorYOffset { get; set; }
        public string anchorIgnoreIfNotPresent { get; set; }
        public string anchorUnits { get; set; }

    }


    private static void WriteStream(Stream reqStream, string str)
    {
        byte[] reqBytes = UTF8Encoding.UTF8.GetBytes(str);
        reqStream.Write(reqBytes, 0, reqBytes.Length);
    }


    private String GetSecurityHeader()
    {
        String str = "";
        str = "<DocuSignCredentials>" + "<Username>" + ConfigurationManager.AppSettings["API.Email"] + "</Username>" +
            "<Password>" + ConfigurationManager.AppSettings["API.Password"] + "</Password>" +
            "<IntegratorKey>" + ConfigurationManager.AppSettings["API.IntegratorKey"] + "</IntegratorKey>" +
            "</DocuSignCredentials>";
        return str;
    }

    public class RecipientViewRequest
    {
        public string authenticationMethod { get; set; }
        public string email { get; set; }
        public string returnUrl { get; set; }
        public string userName { get; set; }
        public string clientUserId { get; set; }
    }


    public class RecipientViewResponse
    {
        public string url { get; set; }
    }

    protected void createEnvelope()
    {


        // Set up the envelope
        CreateEnvelopeRequest createEnvelopeRequest = new CreateEnvelopeRequest();
        createEnvelopeRequest.emailSubject = "Certified Delivery Example";
        createEnvelopeRequest.status = "sent";
        createEnvelopeRequest.emailBlurb = "Example of how certified delivery works";

        // Define first signer 
        CertifiedDelivery signer = new CertifiedDelivery();
        signer.email = email.Value;
        signer.name = firstname.Value + " " + lastname.Value;
        signer.recipientId = 1;
        signer.routingOrder = "1";
        signer.roleName = "Signer1";


        // Define a document 
        Document document = new Document();
        document.documentId = "1";
        document.name = "Sample Form";

        // Define an inline template
        InlineTemplate inline1 = new InlineTemplate();
        inline1.sequence = "2";
        inline1.recipients = new Recipients();
        inline1.recipients.certifiedDeliveries = new List<CertifiedDelivery>();
        inline1.recipients.certifiedDeliveries.Add(signer);


        // Add the inline template to a CompositeTemplate 
        CompositeTemplate compositeTemplate1 = new CompositeTemplate();
        compositeTemplate1.inlineTemplates = new List<InlineTemplate>();
        compositeTemplate1.inlineTemplates.Add(inline1);
        compositeTemplate1.document = document;

        // Add compositeTemplate to the envelope 
        createEnvelopeRequest.compositeTemplates = new List<CompositeTemplate>();
        createEnvelopeRequest.compositeTemplates.Add(compositeTemplate1);


        string output = JsonConvert.SerializeObject(createEnvelopeRequest);

        // Specify a unique boundary string that doesn't appear in the json or document bytes.
        string Boundary = "MY_BOUNDARY";

        // Set the URI
        HttpWebRequest request = HttpWebRequest.Create(ConfigurationManager.AppSettings["DocuSignServer"] + "/restapi/v2/accounts/" + ConfigurationManager.AppSettings["API.AccountID"] + "/envelopes") as HttpWebRequest;

        // Set the method
        request.Method = "POST";

        // Set the authentication header
        request.Headers["Authorization"] = "bearer " + createJWT();

        // Set the overall request content type aand boundary string
        request.ContentType = "multipart/form-data; boundary=" + Boundary;
        request.Accept = "application/json";

        // Start forming the body of the request
        Stream reqStream = request.GetRequestStream();

        // write boundary marker between parts
        WriteStream(reqStream, "\n--" + Boundary + "\n");

        // write out the json envelope definition part
        WriteStream(reqStream, "Content-Type: application/json\n");
        WriteStream(reqStream, "Content-Disposition: form-data\n");
        WriteStream(reqStream, "\n"); // requires an empty line between the header and the json body
        WriteStream(reqStream, output);

        // write out the form bytes for the first form
        WriteStream(reqStream, "\n--" + Boundary + "\n");
        WriteStream(reqStream, "Content-Type: application/pdf\n");
        WriteStream(reqStream, "Content-Disposition: file; filename=\"Sample_Form\"; documentId=1\n");
        WriteStream(reqStream, "\n");
        String filename = uploadFile.Value;
        if (File.Exists(Server.MapPath("~/App_Data/" + filename)))
        {
            // Read the file contents and write them to the request stream
            byte[] buf = new byte[4096];
            int len;
            // read contents of document into the request stream
            FileStream fileStream = File.OpenRead(Server.MapPath("~/App_Data/" + filename));
            while ((len = fileStream.Read(buf, 0, 4096)) > 0)
            {
                reqStream.Write(buf, 0, len);
            }
            fileStream.Close();
        }


        // wrte the end boundary marker - ensure that it is on its own line
        WriteStream(reqStream, "\n--" + Boundary + "--");
        WriteStream(reqStream, "\n");

        try
        {
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                byte[] responseBytes = new byte[response.ContentLength];
                using (var reader = new System.IO.BinaryReader(response.GetResponseStream()))
                {
                    reader.Read(responseBytes, 0, responseBytes.Length);
                }
                string responseText = Encoding.UTF8.GetString(responseBytes);
                CreateEnvelopeResponse createEnvelopeResponse = new CreateEnvelopeResponse();

                createEnvelopeResponse = JsonConvert.DeserializeObject<CreateEnvelopeResponse>(responseText);
                if (createEnvelopeResponse.status.Equals("sent"))
                {
                    Response.Redirect("Confirm.aspx?envelopeID=" + createEnvelopeResponse.envelopeId);
                }
            }
        }
        catch (WebException ex)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;
                using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream(), UTF8Encoding.UTF8))
                {
                    string errorMess = reader.ReadToEnd();
                    log4net.ILog logger = log4net.LogManager.GetLogger(typeof(CertifiedDelivery));
                    logger.Info("\n----------------------------------------\n");
                    logger.Error("DocuSign Error: " + errorMess);
                    logger.Error(ex.StackTrace);
                    Response.Write(ex.Message);
                }
            }
            else
            {
                log4net.ILog logger = log4net.LogManager.GetLogger(typeof(CertifiedDelivery));
                logger.Info("\n----------------------------------------\n");
                logger.Error("WebRequest Error: " + ex.Message);
                logger.Error(ex.StackTrace);
                Response.Write(ex.Message);
            }
        }

    }
}
