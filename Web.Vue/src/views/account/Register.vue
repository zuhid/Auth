<!-------------------------------------------------- script -------------------------------------------------->
<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { accountStore } from '../../store';

const router = useRouter();

/************************* props *************************/
const model = ref({
  firstName: "Test",
  lastName: "User",
  email: "test@example.com",
  phone: "303-789-4563",
  password: "P@ssw0rd"
});
/************************* computed *************************/
/************************* functions *************************/
async function register() {
  await accountStore.register(model.value);
}
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <Card id="register" title="Register" saveText="Register" cancelText="" @onSave="register">
    <Text label="Email" v-model="model.email" />
    <Password label="Password" v-model="model.password" />
    <Text label="First Name" v-model="model.firstName" />
    <Text label="Last Name" v-model="model.lastName" />
    <Text label="Phone" v-model="model.phone" />
    <div class="login-link">
      Already have an account? <router-link to="/account/login">Login</router-link>
    </div>
  </Card>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
#register {
  width: 400px;
}

.login-link {
  margin-top: 1rem;
  text-align: center;
  font-size: 0.9rem;
}

.login-link a {
  color: var(--primary-color);
  text-decoration: none;
  font-weight: var(--font-weight-bold);
}

.login-link a:hover {
  text-decoration: underline;
}
</style>
