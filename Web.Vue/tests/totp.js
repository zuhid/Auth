import { createHmac } from 'crypto';

function base32Decode(base32) {
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
  let bits = 0, value = 0;
  const result = [];
  for (const c of base32.toUpperCase().replace(/\s/g, '')) {
    const idx = alphabet.indexOf(c);
    if (idx < 0) continue;
    value = (value << 5) | idx;
    bits += 5;
    if (bits >= 8) {
      result.push((value >> (bits - 8)) & 0xFF);
      bits -= 8;
    }
  }
  return Buffer.from(result);
}

export function computeTotp(base32Key) {
  const key = base32Decode(base32Key);
  const timestep = Math.floor(Date.now() / 1000 / 30);
  const msg = Buffer.alloc(8);
  msg.writeBigUInt64BE(BigInt(timestep));
  const hash = createHmac('sha1', key).update(msg).digest();
  const offset = hash[hash.length - 1] & 0x0F;
  const code = ((hash[offset] & 0x7F) << 24)
             | ((hash[offset + 1] & 0xFF) << 16)
             | ((hash[offset + 2] & 0xFF) << 8)
             |  (hash[offset + 3] & 0xFF);
  return (code % 1_000_000).toString().padStart(6, '0');
}
