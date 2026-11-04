<!-------------------------------------------------- script -------------------------------------------------->
<script setup>
import { onMounted, ref } from 'vue';
import { accountStore } from '../../store';
import Card from '../../components/Card.vue';

/************************************************** Props **************************************************/
const props = defineProps({
  email: {
    type: String,
    required: true
  },
  token: {
    type: String,
    required: true
  }
});

const status = ref('verifying'); // 'verifying', 'success', 'error'

/************************* emits *************************/
/************************* computed *************************/
/************************* functions *************************/
onMounted(async () => {
  const response = await accountStore.verifyEmail(props.email, props.token);
  if (response) {
    status.value = 'success';
  } else {
    status.value = 'error';
  }
});
</script>

<!-------------------------------------------------- template -------------------------------------------------->
<template>
  <Card id="verify-email" title="Verify Email" saveText="" cancelText="">
    <div v-if="status === 'verifying'">
      <p>Verifying your email, please wait...</p>
    </div>
    <div v-else-if="status === 'success'">
      <p>Your email has been successfully verified! You can now <router-link to="/account/login">login</router-link>.</p>
    </div>
    <div v-else-if="status === 'error'">
      <p>Email verification failed. The link may be invalid or expired.</p>
    </div>
  </Card>
</template>

<!-------------------------------------------------- style -------------------------------------------------->
<style scoped>
#verify-email {
  width: 400px;
}
</style>
