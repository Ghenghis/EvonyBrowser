/**
 * Evony AMF3 Decoder FiddlerScript
 * Decodes AMF3 binary data in Evony traffic and displays readable JSON
 * 
 * Installation:
 * 1. Open Fiddler → Rules → Customize Rules
 * 2. Add this script to the Handlers class
 */

// ============================================================================
// AMF3 Type Constants
// ============================================================================

var AMF3_UNDEFINED = 0x00;
var AMF3_NULL = 0x01;
var AMF3_FALSE = 0x02;
var AMF3_TRUE = 0x03;
var AMF3_INTEGER = 0x04;
var AMF3_DOUBLE = 0x05;
var AMF3_STRING = 0x06;
var AMF3_XML_DOC = 0x07;
var AMF3_DATE = 0x08;
var AMF3_ARRAY = 0x09;
var AMF3_OBJECT = 0x0A;
var AMF3_XML = 0x0B;
var AMF3_BYTEARRAY = 0x0C;
var AMF3_VECTOR_INT = 0x0D;
var AMF3_VECTOR_UINT = 0x0E;
var AMF3_VECTOR_DOUBLE = 0x0F;
var AMF3_VECTOR_OBJECT = 0x10;
var AMF3_DICTIONARY = 0x11;

// ============================================================================
// AMF3 Decoder Class
// ============================================================================

class AMF3Decoder {
    constructor(data) {
        this.data = data;
        this.pos = 0;
        this.stringRefs = [];
        this.objectRefs = [];
        this.traitRefs = [];
    }
    
    readByte() {
        if (this.pos >= this.data.Length) {
            throw new Error("Unexpected end of data");
        }
        return this.data[this.pos++];
    }
    
    readBytes(count) {
        var bytes = [];
        for (var i = 0; i < count; i++) {
            bytes.push(this.readByte());
        }
        return bytes;
    }
    
    readU29() {
        var result = 0;
        var byte;
        
        for (var i = 0; i < 4; i++) {
            byte = this.readByte();
            
            if (i < 3) {
                result = (result << 7) | (byte & 0x7F);
                if ((byte & 0x80) === 0) break;
            } else {
                result = (result << 8) | byte;
            }
        }
        
        return result;
    }
    
    readU29Integer() {
        var value = this.readU29();
        // Sign extend if necessary
        if (value > 0x0FFFFFFF) {
            value = value | 0xE0000000;
        }
        return value;
    }
    
    readDouble() {
        var bytes = this.readBytes(8);
        // Convert to double (big-endian)
        var buffer = new ArrayBuffer(8);
        var view = new DataView(buffer);
        for (var i = 0; i < 8; i++) {
            view.setUint8(i, bytes[i]);
        }
        return view.getFloat64(0, false);
    }
    
    readUTF8(length) {
        var bytes = this.readBytes(length);
        // Convert UTF-8 bytes to string
        var str = "";
        for (var i = 0; i < bytes.length; i++) {
            str += String.fromCharCode(bytes[i]);
        }
        return decodeURIComponent(escape(str));
    }
    
    readString() {
        var ref = this.readU29();
        
        if ((ref & 1) === 0) {
            // Reference
            return this.stringRefs[ref >> 1];
        }
        
        var length = ref >> 1;
        if (length === 0) return "";
        
        var str = this.readUTF8(length);
        this.stringRefs.push(str);
        return str;
    }
    
    readValue() {
        var type = this.readByte();
        
        switch (type) {
            case AMF3_UNDEFINED:
                return undefined;
                
            case AMF3_NULL:
                return null;
                
            case AMF3_FALSE:
                return false;
                
            case AMF3_TRUE:
                return true;
                
            case AMF3_INTEGER:
                return this.readU29Integer();
                
            case AMF3_DOUBLE:
                return this.readDouble();
                
            case AMF3_STRING:
                return this.readString();
                
            case AMF3_DATE:
                return this.readDate();
                
            case AMF3_ARRAY:
                return this.readArray();
                
            case AMF3_OBJECT:
                return this.readObject();
                
            case AMF3_BYTEARRAY:
                return this.readByteArray();
                
            case AMF3_VECTOR_INT:
            case AMF3_VECTOR_UINT:
            case AMF3_VECTOR_DOUBLE:
            case AMF3_VECTOR_OBJECT:
                return this.readVector(type);
                
            case AMF3_DICTIONARY:
                return this.readDictionary();
                
            case AMF3_XML:
            case AMF3_XML_DOC:
                return this.readXML();
                
            default:
                throw new Error("Unknown AMF3 type: " + type);
        }
    }
    
    readDate() {
        var ref = this.readU29();
        
        if ((ref & 1) === 0) {
            return this.objectRefs[ref >> 1];
        }
        
        var timestamp = this.readDouble();
        var date = new Date(timestamp);
        this.objectRefs.push(date);
        return date;
    }
    
    readArray() {
        var ref = this.readU29();
        
        if ((ref & 1) === 0) {
            return this.objectRefs[ref >> 1];
        }
        
        var length = ref >> 1;
        var result = [];
        this.objectRefs.push(result);
        
        // Read associative portion
        var key = this.readString();
        while (key !== "") {
            result[key] = this.readValue();
            key = this.readString();
        }
        
        // Read dense portion
        for (var i = 0; i < length; i++) {
            result.push(this.readValue());
        }
        
        return result;
    }
    
    readObject() {
        var ref = this.readU29();
        
        if ((ref & 1) === 0) {
            return this.objectRefs[ref >> 1];
        }
        
        var traits;
        if ((ref & 2) === 0) {
            // Trait reference
            traits = this.traitRefs[ref >> 2];
        } else {
            // Inline traits
            traits = {
                className: this.readString(),
                dynamic: (ref & 4) !== 0,
                externalizable: (ref & 8) !== 0,
                properties: []
            };
            
            var propCount = ref >> 4;
            for (var i = 0; i < propCount; i++) {
                traits.properties.push(this.readString());
            }
            
            this.traitRefs.push(traits);
        }
        
        var obj = {};
        if (traits.className) {
            obj["__class__"] = traits.className;
        }
        this.objectRefs.push(obj);
        
        if (traits.externalizable) {
            // Read externalized data
            obj["__externalized__"] = this.readValue();
        } else {
            // Read sealed properties
            for (var i = 0; i < traits.properties.length; i++) {
                obj[traits.properties[i]] = this.readValue();
            }
            
            // Read dynamic properties
            if (traits.dynamic) {
                var key = this.readString();
                while (key !== "") {
                    obj[key] = this.readValue();
                    key = this.readString();
                }
            }
        }
        
        return obj;
    }
    
    readByteArray() {
        var ref = this.readU29();
        
        if ((ref & 1) === 0) {
            return this.objectRefs[ref >> 1];
        }
        
        var length = ref >> 1;
        var bytes = this.readBytes(length);
        
        // Convert to hex string for display
        var hex = "";
        for (var i = 0; i < bytes.length; i++) {
            hex += bytes[i].toString(16).padStart(2, "0");
        }
        
        var result = { __type__: "ByteArray", length: length, hex: hex };
        this.objectRefs.push(result);
        return result;
    }
    
    readVector(type) {
        var ref = this.readU29();
        
        if ((ref & 1) === 0) {
            return this.objectRefs[ref >> 1];
        }
        
        var length = ref >> 1;
        var fixed = this.readByte() !== 0;
        
        var result = [];
        this.objectRefs.push(result);
        
        if (type === AMF3_VECTOR_OBJECT) {
            this.readString(); // Object type name
        }
        
        for (var i = 0; i < length; i++) {
            switch (type) {
                case AMF3_VECTOR_INT:
                    result.push(this.readSignedInt());
                    break;
                case AMF3_VECTOR_UINT:
                    result.push(this.readUnsignedInt());
                    break;
                case AMF3_VECTOR_DOUBLE:
                    result.push(this.readDouble());
                    break;
                case AMF3_VECTOR_OBJECT:
                    result.push(this.readValue());
                    break;
            }
        }
        
        return result;
    }
    
    readSignedInt() {
        var bytes = this.readBytes(4);
        var value = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        return value;
    }
    
    readUnsignedInt() {
        var bytes = this.readBytes(4);
        return ((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]) >>> 0;
    }
    
    readDictionary() {
        var ref = this.readU29();
        
        if ((ref & 1) === 0) {
            return this.objectRefs[ref >> 1];
        }
        
        var length = ref >> 1;
        var weakKeys = this.readByte() !== 0;
        
        var result = { __type__: "Dictionary", entries: [] };
        this.objectRefs.push(result);
        
        for (var i = 0; i < length; i++) {
            var key = this.readValue();
            var value = this.readValue();
            result.entries.push({ key: key, value: value });
        }
        
        return result;
    }
    
    readXML() {
        var ref = this.readU29();
        
        if ((ref & 1) === 0) {
            return this.objectRefs[ref >> 1];
        }
        
        var length = ref >> 1;
        var xml = this.readUTF8(length);
        this.objectRefs.push(xml);
        return { __type__: "XML", content: xml };
    }
    
    decode() {
        try {
            return this.readValue();
        } catch (e) {
            return { error: e.message, position: this.pos };
        }
    }
}

// ============================================================================
// Fiddler Integration
// ============================================================================

/**
 * Decode AMF3 data from a session
 */
function decodeAMF3(data) {
    var decoder = new AMF3Decoder(data);
    return decoder.decode();
}

/**
 * Check if data appears to be AMF3
 */
function isAMF3Data(data) {
    if (!data || data.Length < 1) return false;
    
    var firstByte = data[0];
    return firstByte >= 0x00 && firstByte <= 0x11;
}

/**
 * Add decoded AMF3 to session comments
 */
static function OnBeforeResponse(oSession) {
    // Check if this is Evony traffic with AMF content
    var contentType = oSession.ResponseHeaders["Content-Type"];
    if (!contentType) return;
    
    if (contentType.indexOf("application/x-amf") >= 0 || 
        contentType.indexOf("application/octet-stream") >= 0) {
        
        try {
            oSession.utilDecodeResponse();
            var body = oSession.ResponseBody;
            
            if (isAMF3Data(body)) {
                var decoded = decodeAMF3(body);
                var json = Fiddler.WebFormats.JSON.JsonEncode(decoded);
                
                // Store decoded data in session
                oSession["X-AMF3-Decoded"] = json;
                
                // Add to comments
                oSession["ui-comments"] = "AMF3 Decoded - " + json.substring(0, 100) + "...";
                
                FiddlerApplication.Log.LogString("Decoded AMF3: " + json.substring(0, 200));
            }
        } catch (e) {
            oSession["ui-comments"] = "AMF3 Decode Error: " + e.message;
        }
    }
}

/**
 * Context menu to decode selected session
 */
public static ContextAction("Decode AMF3") {
    var sessions = FiddlerApplication.UI.GetSelectedSessions();
    
    for (var i = 0; i < sessions.Length; i++) {
        var session = sessions[i];
        
        try {
            session.utilDecodeResponse();
            var body = session.ResponseBody;
            
            if (body && body.Length > 0) {
                var decoded = decodeAMF3(body);
                var json = Fiddler.WebFormats.JSON.JsonEncode(decoded);
                
                // Show in message box
                FiddlerApplication.UI.ShowMessageBox(json, "Decoded AMF3");
            }
        } catch (e) {
            FiddlerApplication.UI.ShowMessageBox("Error: " + e.message, "Decode Failed");
        }
    }
}

/**
 * Context menu to copy decoded JSON
 */
public static ContextAction("Copy Decoded AMF3") {
    var sessions = FiddlerApplication.UI.GetSelectedSessions();
    
    if (sessions.Length > 0) {
        var decoded = sessions[0]["X-AMF3-Decoded"];
        if (decoded) {
            Utilities.CopyToClipboard(decoded);
            FiddlerApplication.Log.LogString("Copied decoded AMF3 to clipboard");
        }
    }
}
