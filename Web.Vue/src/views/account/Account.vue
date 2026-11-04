<!-------------------------------------------------- script -------------------------------------------------->
<script setup>
import QRCode from 'qrcode';
import { onMounted, ref } from 'vue';
import { accountStore } from '../../store';

/************************* props *************************/
const props = defineProps({
  id: String
});

const model = ref({
  firstName: "",
  lastName: "",
  phone: "",
  phoneVerified: false
});

const token = ref("");
const showTokenInput = ref(false);

const twoFactorInfo = ref(null);
const showTwoFactorSetup = ref(false);
const twoFactorCode = ref("");
const qrCodeUrl = ref("");

/************************* computed *************************/
/************************* functions *************************/
onMounted(async () => model.value = await accountStore.get() ?? {});

async function update() {
  await accountStore.update(model.value);
}

async function verifyPhone() {
  await accountStore.verifyPhone(props.id);
  showTokenInput.value = true;
}

async function confirmPhone() {
  const success = await accountStore.confirmPhone(token.value);
  if (success) {
    model.value.phoneVerified = true;
    showTokenInput.value = false;
  }
}

async function setupTwoFactor() {
  const data = await accountStore.getEnableTwoFactor();
  if (data) {
    twoFactorInfo.value = data;
    showTwoFactorSetup.value = true;
    if (data.authenticatorUri) {
      qrCodeUrl.value = await QRCode.toDataURL(data.authenticatorUri);
    }
  }
}

async function enableTwoFactor() {
  const success = await accountStore.enableTwoFactor(twoFactorCode.value);
  if (success) {
    showTwoFactorSetup.value = false;
    model.value.twoFactorEnabled = true;
  }
}
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <Card id="account" title="Account Settings" saveText="Update" cancelText="" @onSave="update">
    <Text label="First Name" v-model="model.firstName" />
    <Text label="Last Name" v-model="model.lastName" />
    <div class="phone-container">
      <Text label="Phone" v-model="model.phone" />
      <Button v-if="!model.phoneVerified && !showTokenInput" label="Verify" @click="verifyPhone" />
    </div>
    <div v-if="showTokenInput" class="phone-container">
      <Text label="Token" v-model="token" />
      <Button label="Confirm" @click="confirmPhone" />
    </div>
    <hr />
    <div v-if="!model.twoFactorEnabled && !showTwoFactorSetup">
      <Button id="enable_ms_authenticator" @click="setupTwoFactor">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
          <path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm-1 14l-3-3 1.41-1.41L11 12.17l5.59-5.58L18 8l-7 7z"/>
        </svg>
        Enable Microsoft Authenticator
      </Button>
    </div>
    <div v-if="showTwoFactorSetup">
      <p>Scan this QR code or enter the key in your Microsoft Authenticator app:</p>
      <div v-if="qrCodeUrl" class="qr-code">
        <img :src="qrCodeUrl" alt="QR Code" />
      </div>
      <p><b>Key:</b> {{ twoFactorInfo.sharedKey }}</p>
      <Text label="Verification Code" v-model="twoFactorCode" />
      <Button label="Verify & Enable" @click="enableTwoFactor" />
    </div>
    <div v-if="model.twoFactorEnabled">
      <p>Two-factor authentication is enabled.</p>
    </div>
  </Card>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
#account {
  width: 400px;
}

.phone-container {
  display: flex;
  align-items: flex-end;
  gap: var(--gap-lg);
}

.phone-container> :first-child {
  flex: 1;
}

.phone-container> :last-child {
  margin-bottom: var(--gap-lg);
}

.qr-code {
  display: flex;
  justify-content: center;
  margin: 1rem 0;
}

.qr-code img {
  max-width: 200px;
}
</style>
