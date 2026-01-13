/**
 * AMF3 Codec for Evony Protocol
 * Handles encoding and decoding of Action Message Format 3 binary data
 */

// AMF3 Type Markers
const AMF3_UNDEFINED = 0x00;
const AMF3_NULL = 0x01;
const AMF3_FALSE = 0x02;
const AMF3_TRUE = 0x03;
const AMF3_INTEGER = 0x04;
const AMF3_DOUBLE = 0x05;
const AMF3_STRING = 0x06;
const AMF3_XML_DOC = 0x07;
const AMF3_DATE = 0x08;
const AMF3_ARRAY = 0x09;
const AMF3_OBJECT = 0x0A;
const AMF3_XML = 0x0B;
const AMF3_BYTE_ARRAY = 0x0C;
const AMF3_VECTOR_INT = 0x0D;
const AMF3_VECTOR_UINT = 0x0E;
const AMF3_VECTOR_DOUBLE = 0x0F;
const AMF3_VECTOR_OBJECT = 0x10;
const AMF3_DICTIONARY = 0x11;

/**
 * AMF3 Decoder Class
 */
export class AmfDecoder {
  constructor() {
    this.stringRefs = [];
    this.objectRefs = [];
    this.traitRefs = [];
    this.position = 0;
    this.buffer = null;
  }

  /**
   * Decode AMF3 binary data
   * @param {Buffer|Uint8Array} data - Binary data to decode
   * @returns {any} Decoded value
   */
  decode(data) {
    this.buffer = Buffer.isBuffer(data) ? data : Buffer.from(data);
    this.position = 0;
    this.stringRefs = [];
    this.objectRefs = [];
    this.traitRefs = [];
    
    try {
      return this.readValue();
    } catch (error) {
      return {
        error: 'Decode failed',
        message: error.message,
        position: this.position,
        partial: this.getPartialDecode()
      };
    }
  }

  /**
   * Read a single AMF3 value
   */
  readValue() {
    if (this.position >= this.buffer.length) {
      return null;
    }
    
    const marker = this.readU8();
    
    switch (marker) {
      case AMF3_UNDEFINED:
        return undefined;
      case AMF3_NULL:
        return null;
      case AMF3_FALSE:
        return false;
      case AMF3_TRUE:
        return true;
      case AMF3_INTEGER:
        return this.readU29();
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
      case AMF3_BYTE_ARRAY:
        return this.readByteArray();
      case AMF3_XML_DOC:
      case AMF3_XML:
        return this.readXml();
      case AMF3_VECTOR_INT:
        return this.readVectorInt();
      case AMF3_VECTOR_UINT:
        return this.readVectorUint();
      case AMF3_VECTOR_DOUBLE:
        return this.readVectorDouble();
      case AMF3_VECTOR_OBJECT:
        return this.readVectorObject();
      case AMF3_DICTIONARY:
        return this.readDictionary();
      default:
        return { unknownMarker: marker, position: this.position - 1 };
    }
  }

  /**
   * Read unsigned 8-bit integer
   */
  readU8() {
    return this.buffer[this.position++];
  }

  /**
   * Read unsigned 29-bit integer (AMF3 variable-length encoding)
   */
  readU29() {
    let result = 0;
    let byte;
    
    // Read up to 4 bytes
    for (let i = 0; i < 4; i++) {
      byte = this.readU8();
      
      if (i < 3) {
        result = (result << 7) | (byte & 0x7F);
        if ((byte & 0x80) === 0) break;
      } else {
        // Last byte uses all 8 bits
        result = (result << 8) | byte;
      }
    }
    
    return result;
  }

  /**
   * Read signed 29-bit integer
   */
  readI29() {
    const value = this.readU29();
    // Sign extend if negative
    if (value & 0x10000000) {
      return value | 0xE0000000;
    }
    return value;
  }

  /**
   * Read 64-bit double
   */
  readDouble() {
    const value = this.buffer.readDoubleBE(this.position);
    this.position += 8;
    return value;
  }

  /**
   * Read AMF3 string
   */
  readString() {
    const ref = this.readU29();
    
    // Check if reference
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.stringRefs[index] || '';
    }
    
    const length = ref >> 1;
    if (length === 0) return '';
    
    const str = this.buffer.toString('utf8', this.position, this.position + length);
    this.position += length;
    
    this.stringRefs.push(str);
    return str;
  }

  /**
   * Read AMF3 date
   */
  readDate() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const timestamp = this.readDouble();
    const date = new Date(timestamp);
    
    this.objectRefs.push(date);
    return date;
  }

  /**
   * Read AMF3 array
   */
  readArray() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const denseLength = ref >> 1;
    const result = [];
    
    this.objectRefs.push(result);
    
    // Read associative portion (key-value pairs)
    let key = this.readString();
    while (key !== '') {
      result[key] = this.readValue();
      key = this.readString();
    }
    
    // Read dense portion
    for (let i = 0; i < denseLength; i++) {
      result.push(this.readValue());
    }
    
    return result;
  }

  /**
   * Read AMF3 object
   */
  readObject() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const traits = this.readTraits(ref);
    const obj = {};
    
    this.objectRefs.push(obj);
    
    // Add class name if present
    if (traits.className) {
      obj._className = traits.className;
    }
    
    // Read sealed members
    for (const memberName of traits.members) {
      obj[memberName] = this.readValue();
    }
    
    // Read dynamic members
    if (traits.dynamic) {
      let key = this.readString();
      while (key !== '') {
        obj[key] = this.readValue();
        key = this.readString();
      }
    }
    
    return obj;
  }

  /**
   * Read object traits
   */
  readTraits(ref) {
    if ((ref & 3) === 1) {
      // Traits reference
      const index = ref >> 2;
      return this.traitRefs[index] || { members: [], dynamic: false };
    }
    
    const externalizable = (ref & 4) !== 0;
    const dynamic = (ref & 8) !== 0;
    const memberCount = ref >> 4;
    
    const className = this.readString();
    const members = [];
    
    for (let i = 0; i < memberCount; i++) {
      members.push(this.readString());
    }
    
    const traits = { className, externalizable, dynamic, members };
    this.traitRefs.push(traits);
    
    return traits;
  }

  /**
   * Read byte array
   */
  readByteArray() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const length = ref >> 1;
    const bytes = this.buffer.slice(this.position, this.position + length);
    this.position += length;
    
    this.objectRefs.push(bytes);
    return { _type: 'ByteArray', hex: bytes.toString('hex'), length };
  }

  /**
   * Read XML
   */
  readXml() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const length = ref >> 1;
    const xml = this.buffer.toString('utf8', this.position, this.position + length);
    this.position += length;
    
    this.objectRefs.push(xml);
    return { _type: 'XML', content: xml };
  }

  /**
   * Read vector of integers
   */
  readVectorInt() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const length = ref >> 1;
    const fixed = this.readU8() !== 0;
    const result = [];
    
    this.objectRefs.push(result);
    
    for (let i = 0; i < length; i++) {
      result.push(this.buffer.readInt32BE(this.position));
      this.position += 4;
    }
    
    return { _type: 'Vector<int>', fixed, values: result };
  }

  /**
   * Read vector of unsigned integers
   */
  readVectorUint() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const length = ref >> 1;
    const fixed = this.readU8() !== 0;
    const result = [];
    
    this.objectRefs.push(result);
    
    for (let i = 0; i < length; i++) {
      result.push(this.buffer.readUInt32BE(this.position));
      this.position += 4;
    }
    
    return { _type: 'Vector<uint>', fixed, values: result };
  }

  /**
   * Read vector of doubles
   */
  readVectorDouble() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const length = ref >> 1;
    const fixed = this.readU8() !== 0;
    const result = [];
    
    this.objectRefs.push(result);
    
    for (let i = 0; i < length; i++) {
      result.push(this.buffer.readDoubleBE(this.position));
      this.position += 8;
    }
    
    return { _type: 'Vector<Number>', fixed, values: result };
  }

  /**
   * Read vector of objects
   */
  readVectorObject() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const length = ref >> 1;
    const fixed = this.readU8() !== 0;
    const typeName = this.readString();
    const result = [];
    
    this.objectRefs.push(result);
    
    for (let i = 0; i < length; i++) {
      result.push(this.readValue());
    }
    
    return { _type: `Vector<${typeName || 'Object'}>`, fixed, values: result };
  }

  /**
   * Read dictionary
   */
  readDictionary() {
    const ref = this.readU29();
    
    if ((ref & 1) === 0) {
      const index = ref >> 1;
      return this.objectRefs[index];
    }
    
    const length = ref >> 1;
    const weakKeys = this.readU8() !== 0;
    const result = new Map();
    
    this.objectRefs.push(result);
    
    for (let i = 0; i < length; i++) {
      const key = this.readValue();
      const value = this.readValue();
      result.set(key, value);
    }
    
    return { _type: 'Dictionary', weakKeys, entries: Array.from(result.entries()) };
  }

  /**
   * Get partial decode info for error reporting
   */
  getPartialDecode() {
    return {
      bytesRead: this.position,
      totalBytes: this.buffer?.length || 0,
      stringsFound: this.stringRefs.length,
      objectsFound: this.objectRefs.length
    };
  }
}

/**
 * AMF3 Encoder Class
 */
export class AmfEncoder {
  constructor() {
    this.stringRefs = new Map();
    this.objectRefs = new Map();
    this.traitRefs = new Map();
    this.buffer = [];
  }

  /**
   * Encode value to AMF3 binary
   * @param {any} value - Value to encode
   * @returns {Buffer} Encoded binary data
   */
  encode(value) {
    this.stringRefs = new Map();
    this.objectRefs = new Map();
    this.traitRefs = new Map();
    this.buffer = [];
    
    this.writeValue(value);
    return Buffer.from(this.buffer);
  }

  /**
   * Write a single AMF3 value
   */
  writeValue(value) {
    if (value === undefined) {
      this.writeU8(AMF3_UNDEFINED);
    } else if (value === null) {
      this.writeU8(AMF3_NULL);
    } else if (value === false) {
      this.writeU8(AMF3_FALSE);
    } else if (value === true) {
      this.writeU8(AMF3_TRUE);
    } else if (typeof value === 'number') {
      if (Number.isInteger(value) && value >= 0 && value <= 0x1FFFFFFF) {
        this.writeU8(AMF3_INTEGER);
        this.writeU29(value);
      } else {
        this.writeU8(AMF3_DOUBLE);
        this.writeDouble(value);
      }
    } else if (typeof value === 'string') {
      this.writeU8(AMF3_STRING);
      this.writeString(value);
    } else if (value instanceof Date) {
      this.writeU8(AMF3_DATE);
      this.writeDate(value);
    } else if (Array.isArray(value)) {
      this.writeU8(AMF3_ARRAY);
      this.writeArray(value);
    } else if (Buffer.isBuffer(value) || value instanceof Uint8Array) {
      this.writeU8(AMF3_BYTE_ARRAY);
      this.writeByteArray(value);
    } else if (typeof value === 'object') {
      this.writeU8(AMF3_OBJECT);
      this.writeObject(value);
    } else {
      // Fallback to string
      this.writeU8(AMF3_STRING);
      this.writeString(String(value));
    }
  }

  /**
   * Write unsigned 8-bit integer
   */
  writeU8(value) {
    this.buffer.push(value & 0xFF);
  }

  /**
   * Write unsigned 29-bit integer
   */
  writeU29(value) {
    if (value < 0x80) {
      this.writeU8(value);
    } else if (value < 0x4000) {
      this.writeU8((value >> 7) | 0x80);
      this.writeU8(value & 0x7F);
    } else if (value < 0x200000) {
      this.writeU8((value >> 14) | 0x80);
      this.writeU8((value >> 7) | 0x80);
      this.writeU8(value & 0x7F);
    } else {
      this.writeU8((value >> 22) | 0x80);
      this.writeU8((value >> 15) | 0x80);
      this.writeU8((value >> 8) | 0x80);
      this.writeU8(value & 0xFF);
    }
  }

  /**
   * Write 64-bit double
   */
  writeDouble(value) {
    const buf = Buffer.alloc(8);
    buf.writeDoubleBE(value, 0);
    for (let i = 0; i < 8; i++) {
      this.buffer.push(buf[i]);
    }
  }

  /**
   * Write AMF3 string
   */
  writeString(str) {
    if (str === '') {
      this.writeU29(1); // Empty string, inline
      return;
    }
    
    // Check for reference
    if (this.stringRefs.has(str)) {
      const ref = this.stringRefs.get(str);
      this.writeU29(ref << 1); // Reference
      return;
    }
    
    // Store reference
    this.stringRefs.set(str, this.stringRefs.size);
    
    // Write inline
    const encoded = Buffer.from(str, 'utf8');
    this.writeU29((encoded.length << 1) | 1);
    for (let i = 0; i < encoded.length; i++) {
      this.buffer.push(encoded[i]);
    }
  }

  /**
   * Write AMF3 date
   */
  writeDate(date) {
    this.writeU29(1); // Inline
    this.writeDouble(date.getTime());
  }

  /**
   * Write AMF3 array
   */
  writeArray(arr) {
    this.writeU29((arr.length << 1) | 1);
    this.writeU29(1); // Empty associative portion
    
    for (const item of arr) {
      this.writeValue(item);
    }
  }

  /**
   * Write byte array
   */
  writeByteArray(bytes) {
    const buf = Buffer.isBuffer(bytes) ? bytes : Buffer.from(bytes);
    this.writeU29((buf.length << 1) | 1);
    for (let i = 0; i < buf.length; i++) {
      this.buffer.push(buf[i]);
    }
  }

  /**
   * Write AMF3 object
   */
  writeObject(obj) {
    const keys = Object.keys(obj).filter(k => !k.startsWith('_'));
    
    // Write traits inline, dynamic
    const ref = (keys.length << 4) | 0x0B; // memberCount, dynamic=true, inline traits
    this.writeU29(ref);
    
    // Write class name (empty for anonymous)
    this.writeString(obj._className || '');
    
    // Write member names
    for (const key of keys) {
      this.writeString(key);
    }
    
    // Write member values
    for (const key of keys) {
      this.writeValue(obj[key]);
    }
    
    // End dynamic portion
    this.writeString('');
  }
}

export default { AmfDecoder, AmfEncoder };
