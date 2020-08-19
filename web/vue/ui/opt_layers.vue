<template>
  <el-checkbox-group v-model="checkboxGroup1" @change="onChange">
    <el-checkbox-button v-for="(layer,i) in layers" :label="i" :key="i">{{layer}}</el-checkbox-button>
  </el-checkbox-group>
</template>

<script>
import { CheckboxGroup, CheckboxButton } from "element-ui";
import api from "./api-layers";

const components = {
  "el-checkbox-group": CheckboxGroup,
  "el-checkbox-button": CheckboxButton,
};
export default {
  components,
  data() {
    return {
      checkboxGroup1: [],
      layers: [],
    };
  },
  created() {
    api.axiso = this.$http;
    api.get_layers((d) => {
      this.layers = d;
    });
    api.get_on_layers((d) => {
      this.checkboxGroup1 = d;
    });
  },
  methods: {
    onChange(d) {
      api.on_layers_toggled(d, (d) => {});
    },
  },
};
</script>

<style scoped>
</style>