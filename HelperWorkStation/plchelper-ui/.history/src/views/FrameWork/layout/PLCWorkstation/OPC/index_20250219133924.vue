<template>
    <div v-for="item in list" class="box" >{{item}}</div>
</template>
<script setup lang = "ts">
import { ref } from 'vue';

const list = ref(['1-1','1-2','1-3','1-4','2-1','2-2','2-3','2-4'])

</script>
<style>
.box{
    width: 150px;
    height: 150px;
    margin: 50px 0 0 40px;
    font-size: 24px;
    font-weight: 700px;
    line-height: 150px;
    background-color: antiquewhite;
    display: flex; /* 使用 flex 布局 */
    justify-content: center; /* 水平居中 */
    align-items: center; /* 垂直居中 */
}
</style>
