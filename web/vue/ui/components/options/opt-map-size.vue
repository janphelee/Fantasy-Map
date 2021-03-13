<template>
  <el-row>
    <el-button :icon="lock?'el-icon-lock': 'el-icon-unlock'"
               circle
               class="btn-i"
               @click="lock=!lock"></el-button>

    <span>{{label}}:</span>

    <el-input :value="size.w"
              class="input-width-min"
              type="number"
              :disabled="lock"
              @input="on_size_w"
              placeholder="宽度"></el-input>
    <span>x</span>
    <el-input :value="size.h"
              class="input-width-min"
              type="number"
              :disabled="lock"
              @input="on_size_h"
              placeholder="高度"></el-input>
    <slot></slot>
  </el-row>
</template>

<script>
import ElRow from "element-ui/lib/row";
import ElInput from "element-ui/lib/input";
import ElButton from "element-ui/lib/button";

const components = {ElRow, ElInput, ElButton};

export default {
  components,

  model: {
    prop: 'value',
    event: 'input'
  },
  props: {
    label: String,
    value: Object,
  },
  data () {
    return {
      lock: false,
      size: {w: 100, h: 100},
    }
  },
  watch: {
    value (v) {
      this.size.w = v.w;
      this.size.h = v.h;
    },
    size (v) {
      this.$emit('input', v);
    },
  },
  methods: {
    on_size_w (v) {
      if (v < 1) v = 1153
      if (v > 99999) v /= 10;
      this.size.w = v | 0;
    },
    on_size_h (v) {
      if (v < 1) v = 717
      if (v > 99999) v /= 10;
      this.size.h = v | 0;
    },
  }
}
</script>

<style scoped>
.btn-i {
  border: none;
}
.input-width-min {
  width: 72px;
}
</style>

<style>
input::-webkit-outer-spin-button,
input::-webkit-inner-spin-button {
  -webkit-appearance: none;
}
input[type='number'] {
  -moz-appearance: textfield;
}
</style>
