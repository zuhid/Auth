<!-------------------------------------------------- script -------------------------------------------------->
<script setup>
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { accountStore, storage } from '../../store';

const router = useRouter();

/************************* props *************************/
const model = ref({
  email: "admin@example.com",
  password: "P@ssw0rd",
  twoFactorCode: "",
  userId: ""
});
const twoFactorProviders = ref([]);
const selectedProvider = ref(null);

/************************* computed *************************/
const saveText = computed(() => {
  if (selectedProvider.value) return 'Verify';
  if (twoFactorProviders.value.length) return '';
  return 'Login';
});

/************************* functions *************************/
async function handleSave() {
  if (selectedProvider.value) {
    await verifyCode();
  } else if (!twoFactorProviders.value.length) {
    await login();
  }
}

async function login() {
  const response = await accountStore.login(model.value);
  if (response.success) {
    router.push(`/account/account/${storage.userId}`);
  } else if (response.requiresTwoFactor) {
    model.value.userId = response.userId;
    twoFactorProviders.value = response.providers || ['Authenticator', 'Email'];
  }
}

async function selectProvider(provider) {
  selectedProvider.value = provider;
  model.value.twoFactorCode = "";
  if (provider === 'Email' || provider === 'Phone') {
    await accountStore.sendTwoFactorToken(model.value.userId, provider);
  }
}

async function verifyCode() {
  const success = await accountStore.loginTwoFactor(model.value.userId, model.value.twoFactorCode, selectedProvider.value);
  if (success) {
    router.push(`/account/account/${storage.userId}`);
  }
}

function providerLabel(provider) {
  if (provider === 'Authenticator') return 'Authenticator App';
  if (provider === 'Phone') return 'Text Message';
  return provider;
}
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <Card id="login" title="Login" :saveText="saveText" cancelText="" @onSave="handleSave">
    <div v-if="!twoFactorProviders.length">
      <Text label="Email" v-model="model.email" />
      <Password label="Password" v-model="model.password" />
    </div>
    <div v-else-if="!selectedProvider">
      <p>Choose how to verify your identity:</p>
      <div class="provider-buttons">
        <Button v-for="provider in twoFactorProviders" :key="provider" :label="providerLabel(provider)" @click="selectProvider(provider)" />
      </div>
    </div>
    <div v-else>
      <p v-if="selectedProvider === 'Authenticator'">Enter the code from your authenticator app:</p>
      <p v-else>A verification code has been sent to your {{ selectedProvider.toLowerCase() }}.</p>
      <Text label="Verification Code" v-model="model.twoFactorCode" />
    </div>
    <div class="register-link">
      Don't have an account? <router-link to="/account/register">Register</router-link>
    </div>
  </Card>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
#login {
  width: 400px;
}

.register-link {
  margin-top: 1rem;
  text-align: center;
  font-size: 0.9rem;
}

.register-link a {
  color: var(--primary-color);
  text-decoration: none;
  font-weight: var(--font-weight-bold);
}

.register-link a:hover {
  text-decoration: underline;
}

.provider-buttons {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-top: 0.5rem;
}
</style>
