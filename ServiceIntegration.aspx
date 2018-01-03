<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeFile="ServiceIntegration.aspx.cs" Inherits="ServiceIntegration" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="Server">


    <div class="row">
        <div class="col-xs-12">
            <h1><a id="PrefillClick" runat="server" href="#">Service Integration Authentication Example</a></h1>

        </div>
    </div>
    <div class="row" id="AuthInfo" runat="server">
        <div class="col-xs-12">
            <div class="form-group">
                <label for="integratorKey">Integrator Key (client ID)</label>
                <input type="text" runat="server" class="form-control" id="integratorKey" placeholder="">
            </div>
            <div class="form-group">
                <label for="userID">User ID (sub)</label>
                <input type="text" runat="server" class="form-control" id="userID" placeholder="">
            </div>
            <br>
            <div class="form-group">
                <label for="pKey">Private Key</label>
                <textarea rows="25" runat="server" name="pKey" id="pKey" class="form-control" placeholder=""></textarea>
            </div>
            <hr />
        </div>

    </div>
    <div class="row" id="primarySignerSection" runat="server">
        <div class="col-xs-12">
            <h2>Primary Account Holder</h2>
            <div class="form-group">
                <label for="firstname">First Name</label>
                <input type="text" runat="server" class="form-control" id="firstname" placeholder="">
            </div>
            <div class="form-group">
                <label for="lastname">Last Name</label>
                <input type="text" runat="server" class="form-control" id="lastname" placeholder="">
            </div>
            <br>
            <div class="form-group">
                <label for="email">Email Address</label>
                <input type="email" runat="server" class="form-control" id="email" placeholder="">
            </div>
            <hr />
        </div>

    </div>
    <div class="row" id="templates" runat="server">
        <div class="col-xs-12">
            <h2>Envelope Information</h2>
            <div class="form-group">
                <asp:FileUpload ID="FileUpload1" runat="server" />
            </div>
            <div class="form-group">
                <button type="button" visible="true" id="uploadButton" runat="server" class="btn" style="color: #fff; padding: 10px 80px; font-size: 14px; margin: 40px auto; display: block;"></button>
            </div>
            <div class="form-group">
                <label for="uploadFile">Upload File </label>
                <input type="text" runat="server" class="form-control" id="uploadFile" placeholder="" readonly="readonly">
            </div>
            <br />
        </div>
    </div>

    <button type="button" visible="true" id="button"  runat="server" class="btn" style="color: #fff; padding: 10px 80px; font-size: 14px; margin: 40px auto; display: block;"></button>
    <button type="button" visible="true" id="button2" runat="server" class="btn" style="color: #fff; padding: 10px 80px; font-size: 14px; margin: 40px auto; display: block;"></button>


    <!-- Google Analytics -->
    <script>
        (function (b, o, i, l, e, r) {
            b.GoogleAnalyticsObject = l; b[l] || (b[l] =
                function () { (b[l].q = b[l].q || []).push(arguments) }); b[l].l = +new Date;
            e = o.createElement(i); r = o.getElementsByTagName(i)[0];
            e.src = '//www.google-analytics.com/analytics.js';
            r.parentNode.insertBefore(e, r)
        }(window, document, 'script', 'ga'));
        ga('create', 'UA-XXXXX-X', 'auto'); ga('send', 'pageview');
    </script>

    <!-- Scripts -->
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.2/jquery.min.js"></script>
    <script src="../js/main.js"></script>

    <script type='text/javascript' id="__bs_script__">
        document.write("<script async src='//localhost:3000/browser-sync/browser-sync-client.1.9.0.js'><\/script>".replace(/HOST/g, location.hostname).replace(/PORT/g, location.port));
    </script>


</asp:Content>

